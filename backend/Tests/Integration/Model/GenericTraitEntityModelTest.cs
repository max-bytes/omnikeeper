﻿using Autofac;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
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
        public string ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public string? TestAttributeA;

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
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForStringID, string>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestOptionalAttributeHandling()
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            var e1 = new TestEntityForStringID("id1", null);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // overwrite optional attribute, set it
            var e2 = new TestEntityForStringID("id1", "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e2, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var byDataID2 = await model.GetSingleByDataID("id1", layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(e2);

            // re-set to e1, with non-set optional attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
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

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForStringID("id1", "e1"), "id1", true, false);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForStringID("id1", "e1"), "id1");
        }
    }

    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForLongID : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public long ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public string? TestAttributeA;

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
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForLongID, long>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestLongBasedID()
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            var e1 = new TestEntityForLongID(1L, null);
            var e12 = new TestEntityForLongID(2L, "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                await model.InsertOrUpdate(e12, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID(1L, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // get all in a dictionary
            var allByDataID1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            allByDataID1.Should().BeEquivalentTo(new Dictionary<long, TestEntityForLongID>()
            {
                {1L,new TestEntityForLongID(1L, null) },
                {2L,new TestEntityForLongID(2L, "set") },
            });

            // overwrite optional attribute, set it
            var e2 = new TestEntityForLongID(1L, "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e2, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var byDataID2 = await model.GetSingleByDataID(1L, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(e2);

            // re-set to e1, with non-set optional attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
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

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForLongID(1L, "e1"), 1L, true, false);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForLongID(1L, "e1"), 1L);
        }
    }


    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForTupleID : TraitEntity
    {
        [TraitAttribute("id1", "id1")]
        [TraitEntityID]
        public long ID1;
        [TraitAttribute("id2", "id2")]
        [TraitEntityID]
        public string ID2;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public string? TestAttributeA;

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

    class GenericTraitEntityWithTupleIDModelTest : GenericTraitEntityModelTestBase<TestEntityForTupleID, (long, string)>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForTupleID, (long, string)>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestTupleBasedID()
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            var e1 = new TestEntityForTupleID(1L, "id1", null);
            var e12 = new TestEntityForTupleID(1L, "id2", "set");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await model.InsertOrUpdate(e1, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                await model.InsertOrUpdate(e12, layerset, layer1, changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            var byDataID1 = await model.GetSingleByDataID((1L, "id1"), layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(e1);

            // get all in a dictionary
            var allByDataID1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            allByDataID1.Should().BeEquivalentTo(new Dictionary<(long, string), TestEntityForTupleID>()
            {
                {(1L, "id1"),new TestEntityForTupleID(1L, "id1", null) },
                {(1L, "id2"),new TestEntityForTupleID(1L, "id2", "set") },
            });
        }

        [Test]
        public async Task TestIncompleteMatchingTupleID()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();

            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            // create a CI with a partially matching ID
            var ciid = await ciModel.CreateCI(ModelContextBuilder.BuildImmediate());
            await attributeModel.InsertAttribute("id1", new AttributeScalarValueInteger(1L), ciid, layer1, changesetBuilder(), ModelContextBuilder.BuildImmediate(), OtherLayersValueHandlingForceWrite.Instance);

            var non_existant = await model.GetSingleByDataID((1L, "non_existant_id"), layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsTrue(non_existant.Equals(default));
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new TestEntityForTupleID(1L, "test_auth_role01", "foo"),
                () => new TestEntityForTupleID(2L, "test_auth_role02", null),
                (1L, "test_auth_role01"), (2L, "test_auth_role02"), (1L, "non_existant")
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new TestEntityForTupleID(1L, "test_auth_role01", "foo"),
                () => new TestEntityForTupleID(2L, "test_auth_role02", null),
                (1L, "test_auth_role01"), (2L, "test_auth_role02"), (1L, "non_existant")
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityForTupleID(1L, "id1", "e1"),
                () => new TestEntityForTupleID(2L, "id2", "e2"),
                () => new TestEntityForTupleID(2L, "id2", "e2changed"),
                (1L, "id1"), (2L, "id2")
                );
        }

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForTupleID(1L, "id1", "e1"), (1L, "id1"), true, false);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForTupleID(1L, "id1", "e1"), (1L, "id1"));
        }
    }




    [TraitEntity("test_entity4", TraitOriginType.Data)]
    class TestEntityForOutgoingTraitRelation : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public string ID;

        [TraitRelation("tr", "predicate_id", true)]
        public Guid[] TestRelations;

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
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForOutgoingTraitRelation, string>>().WithParameter("jsonSerializer", null!);
        }

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

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { }), "id1", true, true);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForOutgoingTraitRelation("id1", new Guid[] { otherCIID1 }), "id1");
        }
    }


    [TraitEntity("test_entity5", TraitOriginType.Data)]
    class TestEntityForIncomingTraitRelation : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public string ID;

        [TraitRelation("tr", "predicate_id", false)]
        public Guid[] TestRelations;

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
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForIncomingTraitRelation, string>>().WithParameter("jsonSerializer", null!);
        }

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

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForIncomingTraitRelation("id1", new Guid[] { }), "id1", true, true);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForIncomingTraitRelation("id1", new Guid[] { otherCIID1 }), "id1");
        }
    }


    [TraitEntity("test_entity6", TraitOriginType.Data)]
    class TestEntityForPartialEntity : TraitEntity
    {
        [TraitAttribute("id", "test.id")]
        [TraitEntityID]
        public string ID;

        [TraitAttribute("test_attribute_a", "test.test_attribute_a")]
        public string TestAttributeA;

        public TestEntityForPartialEntity()
        {
            ID = "";
            TestAttributeA = "";
        }

        public TestEntityForPartialEntity(string id, string testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithPartialEntityModelTest : GenericTraitEntityModelTestBase<TestEntityForPartialEntity, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForPartialEntity, string>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForPartialEntity("ID1", "foo"), "ID1", false, false);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForPartialEntity("ID1", "foo"), "ID1");
        }
    }







    [TraitEntity("test_entity1", TraitOriginType.Data)]
    class TestEntityForDouble : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public long ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a")]
        public double TestAttributeA;

        public TestEntityForDouble()
        {
            ID = 0L;
            TestAttributeA = 0.0;
        }

        public TestEntityForDouble(long id, double testAttributeA)
        {
            ID = id;
            TestAttributeA = testAttributeA;
        }
    }

    class GenericTraitEntityWithDoubleModelTest : GenericTraitEntityModelTestBase<TestEntityForDouble, long>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityForDouble, long>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new TestEntityForDouble(1L, -1.2),
                () => new TestEntityForDouble(2L, 3.1),
                1L, 2L, 3L
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new TestEntityForDouble(1L, -1.2),
                () => new TestEntityForDouble(2L, 3.1),
                1L, 2L, 3L
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityForDouble(1L, -1.2),
                () => new TestEntityForDouble(2L, 3.1),
                () => new TestEntityForDouble(2L, 0.0),
                1L, 2L
                );
        }

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityForDouble(1L, -912.12), 1L, false, false);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityForDouble(1L, 34.12), 1L);
        }
    }



    [TraitEntity("test_entity9", TraitOriginType.Data)]
    class TestEntityWithOptionalInteger : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public long ID;

        [TraitAttribute("test_attribute_a", "test_attribute_a", optional: true)]
        public long? TestAttributeA;

        public TestEntityWithOptionalInteger()
        {
            ID = 0L;
            TestAttributeA = null;
        }
        public TestEntityWithOptionalInteger(long id, long? a)
        {
            ID = id;
            TestAttributeA = a;
        }
    }

    class GenericTraitEntityWithOptionalIntegerModelTest : GenericTraitEntityModelTestBase<TestEntityWithOptionalInteger, long>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityWithOptionalInteger, long>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestGenericOperations()
        {
            await TestGenericModelOperations(
                () => new TestEntityWithOptionalInteger(1L, -2L),
                () => new TestEntityWithOptionalInteger(2L, null),
                1L, 2L, 3L
                );
        }
        [Test]
        public async Task TestGetByDataID()
        {
            await TestGenericModelGetByDataID(
                () => new TestEntityWithOptionalInteger(1L, -2L),
                () => new TestEntityWithOptionalInteger(2L, null),
                1L, 2L, 3L
                );
        }

        [Test]
        public async Task TestBulkReplace()
        {
            await TestGenericModelBulkReplace(
                () => new TestEntityWithOptionalInteger(1L, -2L),
                () => new TestEntityWithOptionalInteger(2L, -3L),
                () => new TestEntityWithOptionalInteger(2L, null),
                1L, 2L
                );
        }

        [Test]
        public async Task TestUpdateIncompleteTraitEntity()
        {
            await TestGenericModelUpdateIncompleteTraitEntity(() => new TestEntityWithOptionalInteger(1L, null), 1L, true, true);
        }

        [Test]
        public async Task TestOtherLayersValueHandling()
        {
            await TestGenericModelOtherLayersValueHandling(() => new TestEntityWithOptionalInteger(1L, null), 1L);
        }
    }



    [TraitEntity("test_entity7", TraitOriginType.Data)]
    class TestEntityWithSingularTraitRelation : TraitEntity
    {
        [TraitAttribute("id", "id")]
        [TraitEntityID]
        public string ID;

        [TraitRelation("tr", "predicate_id", false)]
        public Guid? TestRelation;

        public TestEntityWithSingularTraitRelation()
        {
            ID = "";
            TestRelation = null;
        }

        public TestEntityWithSingularTraitRelation(string id, Guid? testRelation)
        {
            ID = id;
            TestRelation = testRelation;
        }
    }

    class GenericTraitEntityWithSingularTraitRelationTest : GenericTraitEntityModelTestBase<TestEntityWithSingularTraitRelation, string>
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            builder.RegisterType<GenericTraitEntityModel<TestEntityWithSingularTraitRelation, string>>().WithParameter("jsonSerializer", null!);
        }

        [Test]
        public async Task TestGenericOperations()
        {
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var otherCIID1 = await ciModel.CreateCI(modelContextBuilder.BuildImmediate());

            await TestGenericModelOperations(
                () => new TestEntityWithSingularTraitRelation("id1", otherCIID1),
                () => new TestEntityWithSingularTraitRelation("id2", null),
                "id1", "id2", "non_existant"
                );
        }
    }
}
