using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntity1 : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntity1()
        {
            ID = "";
            TestAttributeA = "";
        }

        public TestEntity1(string id, string? testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithStringIDModelTest : GenericTraitEntityModelTestBase<TestEntity1, string>
    {

        [Test]
        public async Task TestOptionalAttributeHandling()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntity1("id1", null);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // overwrite optional attribute, set it
            var e2 = new TestEntity1("id1", "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e2, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID2 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(e2);

            // re-set to e1, with non-set optional attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            // ensure that the optional attribute is now gone again as well
            var byDataID3 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID3.entity.Should().BeEquivalentTo(e1);
        }
    }

    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntity2 : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly long ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntity2()
        {
            ID = 0L;
            TestAttributeA = "";
        }

        public TestEntity2(long id, string? testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithLongIDModelTest : GenericTraitEntityModelTestBase<TestEntity2, long>
    {
        [Test]
        public async Task TestLongBasedID()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntity2(1L, null);
            var e12 = new TestEntity2(2L, "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                await model.InsertOrUpdate(e12, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID(1L, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // get all in a dictionary
            var allByDataID1 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            allByDataID1.Should().BeEquivalentTo(new Dictionary<long, TestEntity2>()
            {
                {1L,new TestEntity2(1L, null) },
                {2L,new TestEntity2(2L, "set") },
            });

            // overwrite optional attribute, set it
            var e2 = new TestEntity2(1L, "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e2, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID2 = await model.GetSingleByDataID(1L, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(e2);

            // re-set to e1, with non-set optional attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            // ensure that the optional attribute is now gone again as well
            var byDataID3 = await model.GetSingleByDataID(1L, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID3.entity.Should().BeEquivalentTo(e1);
        }
    }


    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntity3 : TraitEntity
    {
        [TraitAttribute("id1", "id1")]
        [TraitEntityID]
        public readonly long ID1;
        [TraitAttribute("id2", "id2")]
        [TraitEntityID]
        public readonly string ID2;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntity3()
        {
            ID1 = 0L;
            ID2 = "";
            TestAttributeA = "";
        }

        public TestEntity3(long id1, string id2, string? testAttributeA)
        {
            ID1 = id1;
            ID2 = id2;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithTupleIDModelTest : GenericTraitEntityModelTestBase<TestEntity3, Tuple<long, string>>
    {
        [Test]
        public async Task TestTupleBasedID()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntity3(1L, "id1", null);
            var e12 = new TestEntity3(1L, "id2", "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                await model.InsertOrUpdate(e12, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID(Tuple.Create(1L, "id1"), layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // get all in a dictionary
            var allByDataID1 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            allByDataID1.Should().BeEquivalentTo(new Dictionary<Tuple<long, string>, TestEntity3>()
            {
                {Tuple.Create(1L, "id1"),new TestEntity3(1L, "id1", null) },
                {Tuple.Create(1L, "id2"),new TestEntity3(1L, "id2", "set") },
            });
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new TestEntity3(1L, "test_auth_role01", "foo"),
                () => new TestEntity3(2L, "test_auth_role02", null),
                Tuple.Create(1L, "test_auth_role01"), Tuple.Create(2L, "test_auth_role02"), Tuple.Create(1L, "non_existant")
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new TestEntity3(1L, "test_auth_role01", "foo"),
                () => new TestEntity3(2L, "test_auth_role02", null),
                Tuple.Create(1L, "test_auth_role01"), Tuple.Create(2L, "test_auth_role02"), Tuple.Create(1L, "non_existant")
                );
        }
    }
}
