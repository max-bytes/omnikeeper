using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForStringID : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntityForStringID()
        {
            ID = "";
            TestAttributeA = "";
        }

        public TestEntityForStringID(string id, string? testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithStringIDModelTest : GenericTraitEntityModelTestBase<TestEntityForStringID, string>
    {

        [Test]
        public async Task TestOptionalAttributeHandling()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntityForStringID("id1", null);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // overwrite optional attribute, set it
            var e2 = new TestEntityForStringID("id1", "set");
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

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityForStringID("id1", "e1"),
                () => new TestEntityForStringID("id2", "e2"),
                () => new TestEntityForStringID("id2", "e2changed"),
                "id1", "id2"
                );
        }
        
    }

    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForLongID : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly long ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntityForLongID()
        {
            ID = 0L;
            TestAttributeA = "";
        }

        public TestEntityForLongID(long id, string? testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithLongIDModelTest : GenericTraitEntityModelTestBase<TestEntityForLongID, long>
    {
        [Test]
        public async Task TestLongBasedID()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntityForLongID(1L, null);
            var e12 = new TestEntityForLongID(2L, "set");
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
            allByDataID1.Should().BeEquivalentTo(new Dictionary<long, TestEntityForLongID>()
            {
                {1L,new TestEntityForLongID(1L, null) },
                {2L,new TestEntityForLongID(2L, "set") },
            });

            // overwrite optional attribute, set it
            var e2 = new TestEntityForLongID(1L, "set");
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

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityForLongID(1L, "e1"),
                () => new TestEntityForLongID(2L, "e2"),
                () => new TestEntityForLongID(2L, "e2changed"),
                1L, 2L
                );
        }
    }


    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForTupleID : TraitEntity
    {
        [TraitAttribute("id1", "id1")]
        [TraitEntityID]
        public readonly long ID1;
        [TraitAttribute("id2", "id2")]
        [TraitEntityID]
        public readonly string ID2;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public readonly string? TestAttributeA;

        public TestEntityForTupleID()
        {
            ID1 = 0L;
            ID2 = "";
            TestAttributeA = "";
        }

        public TestEntityForTupleID(long id1, string id2, string? testAttributeA)
        {
            ID1 = id1;
            ID2 = id2;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithTupleIDModelTest : GenericTraitEntityModelTestBase<TestEntityForTupleID, Tuple<long, string>>
    {
        [Test]
        public async Task TestTupleBasedID()
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var e1 = new TestEntityForTupleID(1L, "id1", null);
            var e12 = new TestEntityForTupleID(1L, "id2", "set");
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
            allByDataID1.Should().BeEquivalentTo(new Dictionary<Tuple<long, string>, TestEntityForTupleID>()
            {
                {Tuple.Create(1L, "id1"),new TestEntityForTupleID(1L, "id1", null) },
                {Tuple.Create(1L, "id2"),new TestEntityForTupleID(1L, "id2", "set") },
            });
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new TestEntityForTupleID(1L, "test_auth_role01", "foo"),
                () => new TestEntityForTupleID(2L, "test_auth_role02", null),
                Tuple.Create(1L, "test_auth_role01"), Tuple.Create(2L, "test_auth_role02"), Tuple.Create(1L, "non_existant")
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new TestEntityForTupleID(1L, "test_auth_role01", "foo"),
                () => new TestEntityForTupleID(2L, "test_auth_role02", null),
                Tuple.Create(1L, "test_auth_role01"), Tuple.Create(2L, "test_auth_role02"), Tuple.Create(1L, "non_existant")
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityForTupleID(1L, "id1", "e1"),
                () => new TestEntityForTupleID(2L, "id2", "e2"),
                () => new TestEntityForTupleID(2L, "id2", "e2changed"),
                Tuple.Create(1L, "id1"), Tuple.Create(2L, "id2")
                );
        }
    }




    [TraitEntity("test_entity4", TraitOriginType.Data)]
    class TestEntityForOutgoingTraitRelation : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitRelation("tr", "predicate_id", true, 1, -1)]
        public readonly Guid[] TestRelations;

        public TestEntityForOutgoingTraitRelation()
        {
            ID = "";
            TestRelations = new Guid[0];
        }

        public TestEntityForOutgoingTraitRelation(string id, Guid[] testRelations)
        {
            ID = id;
            TestRelations = testRelations;
        }
    }

    class GenericTraitEntityWithTraitRelationTest : GenericTraitEntityModelTestBase<TestEntityForOutgoingTraitRelation, string>
    {
        [Test]
        public async Task TestGenericOperations()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelOperations(
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForOutgoingTraitRelation("id2", new Guid[] { otherCIID2 }),
                "id1", "id2", "non_existant"
                );
        }

        [Test]
        public async Task TestGenericChangeAdd()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelChange(
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1 }),
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                "id1"
                );
        }

        [Test]
        public async Task TestGenericChangeRemove()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelChange(
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID2 }),
                "id1"
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelBulkReplace(
                () => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForOutgoingTraitRelation("id2", new Guid[] { otherCIID1 }),
                () => new TestEntityForOutgoingTraitRelation("id2", new Guid[] { otherCIID2 }),
                "id1", "id2"
                );
        }
    }




    [TraitEntity("test_entity5", TraitOriginType.Data)]
    class TestEntityForIncomingTraitRelation : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitRelation("tr", "predicate_id", false, 1, -1)]
        public readonly Guid[] TestRelations;

        public TestEntityForIncomingTraitRelation()
        {
            ID = "";
            TestRelations = new Guid[0];
        }

        public TestEntityForIncomingTraitRelation(string id, Guid[] testRelations)
        {
            ID = id;
            TestRelations = testRelations;
        }
    }

    class GenericTraitEntityWithIncomingTraitRelationTest : GenericTraitEntityModelTestBase<TestEntityForIncomingTraitRelation, string>
    {
        [Test]
        public async Task TestGenericOperations()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelOperations(
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForIncomingTraitRelation("id2", new Guid[] { otherCIID2 }),
                "id1", "id2", "non_existant"
                );
        }

        [Test]
        public async Task TestGenericChangeAdd()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelChange(
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1 }),
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                "id1"
                );
        }

        [Test]
        public async Task TestGenericChangeRemove()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelChange(
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID2 }),
                "id1"
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());
            var otherCIID2 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelBulkReplace(
                () => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1, otherCIID2 }),
                () => new TestEntityForIncomingTraitRelation("id2", new Guid[] { otherCIID1 }),
                () => new TestEntityForIncomingTraitRelation("id2", new Guid[] { otherCIID2 }),
                "id1", "id2"
                );
        }
    }
}
