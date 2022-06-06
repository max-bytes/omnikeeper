using Autofac;
using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class MaskingTraitEntitiesTest : DIServicedTestBase
    {
        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            // add controller
            builder.RegisterType<GenericTraitEntityModel<TestEntity1, string>>().WithParameter("jsonSerializer", null!).InstancePerLifetimeScope();
            builder.RegisterType<GenericTraitEntityModel<TestEntity2, string>>().WithParameter("jsonSerializer", null!).InstancePerLifetimeScope();
        }

        [Test]
        public async Task TestBasics()
        {
            var em = GetService<GenericTraitEntityModel<TestEntity1, string>>();

            var transI = ModelContextBuilder.BuildImmediate();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", transI);

            var layerset1 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);
            var layerset2 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l2" }, transI);
            var layerset12 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1", "l2" }, transI);
            var layerset21 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l2", "l1" }, transI);


            // write two entities into l2
            var e1 = new TestEntity1("id1", "name1");
            var e2 = new TestEntity1("id2", null);
            using (var trans = ModelContextBuilder.BuildDeferred()) {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1, layerset12, layer2.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                await em.InsertOrUpdate(e2, layerset12, layer2.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }
            var r1 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r1.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // update one entity, writing to l1
            var e1U = new TestEntity1("id1", "name1Updated");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1U, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the update
            var r2 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r2.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1U },
                {"id2", e2 }
            });
            // reading from both layers in order l2,l1 should NOT contain the update
            var r3 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset21, transI, TimeThreshold.BuildLatest());
            r3.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // delete optional attribute from e1
            var e1UU = new TestEntity1("id1", null);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1UU, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the update again, masking the underlying attribute of entity e1
            var r4 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r4.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1UU },
                {"id2", e2 }
            });

            // delete whole entity e2
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.TryToDelete(e2.ID, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the deletion
            var r5 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r5.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1UU }
            });

            // reading from both layers in order l2,l1 should STILL return the old state
            var r6 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset21, transI, TimeThreshold.BuildLatest());
            r6.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // insert a new whole entity e3, in l1
            var e3 = new TestEntity1("id3", "name3");
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e3, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the addition
            var r7 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r7.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1UU },
                {"id3", e3 }
            });
            // reading from both layers in order l2,l1 should also return the new addition, but not the deletion or the other changes to e1
            var r8 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset21, transI, TimeThreshold.BuildLatest());
            r8.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1 },
                {"id2", e2 },
                {"id3", e3 }
            });
            // reading from layer l2 only should not show any changes to l1
            var r9 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset2, transI, TimeThreshold.BuildLatest());
            r9.Should().BeEquivalentTo(new Dictionary<string, TestEntity1>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });
        }

        [TraitEntity("test_entity", TraitOriginType.Data)]
        private class TestEntity1 : TraitEntity
        {
            [TraitEntityID]
            [TraitAttribute("id", "ta.id")]
            public string ID;

            [TraitAttribute("name", "ta.name", optional: true)]
            public string? Name;

            public TestEntity1()
            {
                ID = "";
                Name = "";
            }

            public TestEntity1(string id, string? name)
            {
                ID = id;
                Name = name;
            }
        }


        [Test]
        public async Task TestRelations()
        {
            var em = GetService<GenericTraitEntityModel<TestEntity2, string>>();

            var transI = ModelContextBuilder.BuildImmediate();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", transI);

            //var layerset1 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1" }, transI);
            //var layerset2 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l2" }, transI);
            var layerset12 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l1", "l2" }, transI);
            var layerset21 = await GetService<ILayerModel>().BuildLayerSet(new string[] { "l2", "l1" }, transI);

            var relatedCIIDs = new Guid[]
            {
                await GetService<ICIModel>().CreateCI(transI),
                await GetService<ICIModel>().CreateCI(transI),
            };

            // write two entities into l2
            var e1 = new TestEntity2("id1", relatedCIIDs);
            var e2 = new TestEntity2("id2", new Guid[0]);
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1, layerset12, layer2.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                await em.InsertOrUpdate(e2, layerset12, layer2.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }
            var r1 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r1.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // update one entity, writing to l1
            var e1U = new TestEntity2("id1", relatedCIIDs.Skip(1).ToArray());
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1U, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the update
            var r2 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r2.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1U },
                {"id2", e2 }
            });
            // reading from both layers in order l2,l1 should NOT contain the update
            var r3 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset21, transI, TimeThreshold.BuildLatest());
            r3.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // delete whole entity e2
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.TryToDelete(e2.ID, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the deletion
            var r4 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r4.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1U }
            });
            // reading from both layers in order l2,l1 should STILL return the old state
            var r5 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset21, transI, TimeThreshold.BuildLatest());
            r5.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1 },
                {"id2", e2 }
            });

            // update entity e1 again, adding new relations
            var additionalRelatedCIID = await GetService<ICIModel>().CreateCI(transI);
            var e1UU = new TestEntity2("id1", relatedCIIDs.Concat(new Guid[] { additionalRelatedCIID }).ToArray());
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await em.InsertOrUpdate(e1UU, layerset12, layer1.ID, new DataOriginV1(DataOriginType.Manual), changeset, trans, MaskHandlingForRemovalApplyMaskIfNecessary.Build(layerset12, layer1.ID));
                trans.Commit();
            }
            // reading from both layers in order l1,l2 should reflect the update
            var r6 = await em.GetByDataID(AllCIIDsSelection.Instance, layerset12, transI, TimeThreshold.BuildLatest());
            r6.Should().BeEquivalentTo(new Dictionary<string, TestEntity2>()
            {
                {"id1", e1UU },
            });
        }

        [TraitEntity("test_entity2", TraitOriginType.Data)]
        private class TestEntity2 : TraitEntity
        {
            [TraitEntityID]
            [TraitAttribute("id", "ta.id")]
            public string ID;

            [TraitRelation("related", "predicate", true)]
            public Guid[] Related;

            public TestEntity2()
            {
                ID = "";
                Related = new Guid[0];
            }

            public TestEntity2(string id, Guid[] related)
            {
                ID = id;
                Related = related;
            }
        }
    }
}
