using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    public abstract class GenericTraitEntityModelTestBase<T, ID> : DIServicedTestBase where T : TraitEntity, new() where ID : notnull, IEquatable<ID>
    {
        public GenericTraitEntityModelTestBase() : base(false, false)
        {
        }

        protected async Task TestGenericModelChange(Func<T> creator1, Func<T> creator1Changed, ID id1)
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    (entityOut, _, _) = await model.InsertOrUpdate(entityIn,
                        layerset, layer1,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            var byDataID1 = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(entity1);
            var getAllByDataID = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataID.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1.entity },
            });


            var entity1Updated = await Insert(creator1Changed());
            var byDataID1Updated = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1Updated.entity.Should().BeEquivalentTo(entity1Updated);
            var getAllByDataIDUpdated = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataIDUpdated.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1Updated.entity },
            });
        }

        protected async Task TestGenericModelGetByDataID(Func<T> creator1, Func<T> creator2, ID id1, ID id2, ID nonExistentID)
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    (entityOut, _, _) = await model.InsertOrUpdate(entityIn,
                        layerset, layer1,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            var entity2 = await Insert(creator2());

            var byDataID1 = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(entity1, opt => opt.ComparingByMembers<JsonElement>());
            var byDataID2 = await model.GetSingleByDataID(id2, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(entity2, opt => opt.ComparingByMembers<JsonElement>());
            var byNonExistentID = await model.GetSingleByDataID(nonExistentID, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(ValueTuple.Create<T?, Guid>(null, default), byNonExistentID);

            var getAllByDataID = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataID.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1.entity },
                { id2, byDataID2.entity },
            }, opt => opt.ComparingByMembers<JsonElement>());
        }

        protected async Task TestGenericModelOperations(Func<T> creator1, Func<T> creator2, ID entity1ID, ID entity2ID, ID nonExistantID)
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            var rt1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            async Task<T> Insert(T entityIn, IChangesetProxy changesetProxy, bool expectChange = true)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    bool changed;
                    (entityOut, changed, _) = await model.InsertOrUpdate(entityIn,
                        layerset, layer1,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy, trans, MaskHandlingForRemovalApplyNoMask.Instance);

                    Assert.IsNotNull(entityOut);
                    Assert.AreEqual(expectChange, changed);

                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1(), changesetBuilder());
            entity1.Should().BeEquivalentTo(creator1(), opt => opt.ComparingByMembers<JsonElement>());

            var rt2 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt2.Count());
            rt2.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 } }, options => options.WithoutStrictOrdering().ComparingByMembers<JsonElement>());

            var entity2 = await Insert(creator2(), changesetBuilder());
            entity2.Should().BeEquivalentTo(creator2(), opt => opt.ComparingByMembers<JsonElement>());

            var rt3 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt3.Count());
            rt3.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering().ComparingByMembers<JsonElement>());

            // delete using non existant ID
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(nonExistantID, layerset, layer1,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsFalse(result);
            }

            // delete one of the existing ones
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(entity1ID, layerset, layer1,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsTrue(result);

                trans.Commit();
            }

            var rt4 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt4.Count());
            rt4.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity2ID, entity2 } }, options => options.WithoutStrictOrdering().ComparingByMembers<JsonElement>());

            // re-insert
            var entity1Re = await Insert(creator1(), changesetBuilder());
            entity1Re.Should().BeEquivalentTo(creator1(), opt => opt.ComparingByMembers<JsonElement>());

            var rt5 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt5.Count());
            rt5.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering().ComparingByMembers<JsonElement>());


            // try to insert same again
            var entity1Re2 = await Insert(creator1(), changesetBuilder(), false);
            entity1Re2.Should().BeEquivalentTo(creator1(), opt => opt.ComparingByMembers<JsonElement>());

            var rt6 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt6.Count());
            rt6.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering().ComparingByMembers<JsonElement>());

        }


        protected async Task TestGenericModelUpdateIncompleteTraitEntity(Func<T> creator1, ID entity1ID, bool areIDAttributesEqualToAllRequiredAttributes, bool areIDAttributesEqualToAllAttributes)
        {
            var (model, layer1, layer2, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1, layer2);

            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();

            var rt1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            // create a CI with attributes that match the ID attributes of entity1, but not the full trait entity
            var ciid = await ciModel.CreateCI(ModelContextBuilder.BuildImmediate());
            var idAttributeInfos = GenericTraitEntityHelper.ExtractIDAttributeInfos<T, ID>();
            var idAttributeValues = idAttributeInfos.ExtractAttributeValuesFromID(entity1ID);
            var idAttributeNames = idAttributeInfos.GetIDAttributeNames();
            for(var i = 0;i < idAttributeNames.Length;i++)
            {
                await attributeModel.InsertAttribute(idAttributeNames[i], idAttributeValues[i], ciid, layer2, changesetBuilder(), new DataOriginV1(DataOriginType.Manual), ModelContextBuilder.BuildImmediate(), OtherLayersValueHandlingForceWrite.Instance);
            }

            var rt2 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            if (areIDAttributesEqualToAllRequiredAttributes)
            {
                Assert.AreEqual(1, rt2.Count);
            } else
            {
                Assert.IsEmpty(rt2);
            }

            T? entity1 = null;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changed = false;
                (entity1, changed, _) = await model.InsertOrUpdate(creator1(),
                    layerset, layer1,
                    new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsNotNull(entity1);
                if (areIDAttributesEqualToAllAttributes)
                    Assert.IsFalse(changed);
                else
                    Assert.IsTrue(changed);
                trans.Commit();
            }

            // entity should be created at the same CIID as the attributes that together form a matching ID
            var rt3 = await model.GetByCIID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt3.Count());
            rt3.Should().BeEquivalentTo(new Dictionary<Guid, T>() { { ciid, creator1() } }, options => options.WithoutStrictOrdering());

            // no new CI should have been created
            var allCIIDs = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());
            allCIIDs.Should().BeEquivalentTo(new List<Guid>() { ciid });

            // empty layer1
            var removed = await model.TryToDelete(entity1ID, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            var rt4 = await model.GetByCIID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            if (areIDAttributesEqualToAllRequiredAttributes)
            {
                // because the ID attributes we inserted in layer2 constitute a proper entity, the entity is not actually removed and still exists, even though we removed its attributes on layer1
                Assert.IsFalse(removed); 
                rt4.Keys.Should().BeEquivalentTo(new List<Guid>() { ciid }, options => options.WithoutStrictOrdering());
            }
            else
            {
                // after our removal from layer1, there is no proper entity anymore, and the removal returns true (=it successfully removed the entity)
                Assert.IsEmpty(rt4);
                Assert.IsTrue(removed);
            }

            // insert again, this time with bulk
            var c1 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            if (areIDAttributesEqualToAllAttributes) // there should only be any changes iff the id attributes are not equal to the full set of attributes
                Assert.IsFalse(c1);
            else
                Assert.IsTrue(c1);
            var rt5 = await model.GetByCIID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt5.Count());

            // no new CI should have been created
            var allCIIDs2 = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());
            allCIIDs2.Should().BeEquivalentTo(new List<Guid>() { ciid });
        }

        protected async Task TestGenericModelOtherLayersValueHandling(Func<T> creator1, ID entity1ID)
        {
            var (model, layer1, layer2, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1, layer2);

            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();

            var rt1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            // insert entity1 into layer2
            T? entity1 = null;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changed = false;
                (entity1, changed, _) = await model.InsertOrUpdate(creator1(),
                    layerset, layer2,
                    new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsNotNull(entity1);
                Assert.IsTrue(changed);
                trans.Commit();
            }

            var rt3 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt3.Count());
            rt3.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, creator1() } }, options => options.WithoutStrictOrdering());

            // insert entity1 into layer1 as well
            // because of the way trait entities are handled regarding other-layers-values, no changes must occur
            T? entity1Again = null;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changed = false;
                (entity1Again, changed, _) = await model.InsertOrUpdate(creator1(),
                    layerset, layer1,
                    new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsNotNull(entity1);
                Assert.IsFalse(changed); // no change must occur
                trans.Commit();
            }
        }


        protected async Task TestGenericModelBulkReplace(Func<T> creator1, Func<T> creator2, Func<T> creator2Changed, ID entity1ID, ID entity2ID)
        {
            var (model, layer1, _, changesetBuilder) = await SetupModel();
            var layerset = new LayerSet(layer1);

            var rt1 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            var entity1 = creator1();
            var entity2 = creator2();
            var entity2Changed = creator2Changed();


            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var ciidsAtStart = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());

            // initial insert
            // +2 CIs
            var c1 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsTrue(c1);
            var rt2 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt2.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // same operation again, nothing must change
            var c2 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsFalse(c2);
            var rt3 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt3.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // remove one entity
            var c3 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity2ID, entity2 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsTrue(c3);
            var rt4 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt4.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // completely different set
            // +1 CIs
            var c4 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsTrue(c4);
            var rt5 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt5.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 } }, options => options.WithoutStrictOrdering());

            // add missing entity again
            // +1 CIs
            var c5 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsTrue(c5);
            var rt6 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt6.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());


            // change one entity
            var c6 = await model.BulkReplace(AllCIIDsSelection.Instance, new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2Changed } }, layerset, layer1, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate(), MaskHandlingForRemovalApplyNoMask.Instance);
            Assert.IsTrue(c6);
            var rt7 = await model.GetByDataID(AllCIIDsSelection.Instance, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt7.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2Changed } }, options => options.WithoutStrictOrdering());


            // make sure of the correct amount of created CIs
            var ciids = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());
            Assert.AreEqual(4, ciids.Count() - ciidsAtStart.Count());
        }

        protected async Task<(GenericTraitEntityModel<T, ID> model, string layerID1, string layerID2, Func<IChangesetProxy> changesetBuilder)> SetupModel()
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await ServiceProvider.GetRequiredService<ILayerModel>().CreateLayerIfNotExists("testlayer1", ModelContextBuilder.BuildImmediate());
            var (layer2, _) = await ServiceProvider.GetRequiredService<ILayerModel>().CreateLayerIfNotExists("testlayer2", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[] { new AuthRole("ar1", new string[] {
                    PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1),
                    PermissionUtils.GetLayerReadPermission(layer2), PermissionUtils.GetLayerWritePermission(layer2),
                })
            });
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            //var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            //var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            //var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            //var relationModel = ServiceProvider.GetRequiredService<IRelationModel>();

            var model = ServiceProvider.GetRequiredService<GenericTraitEntityModel<T, ID>>();// new GenericTraitEntityModel<T, ID>(effectiveTraitModel, ciModel, attributeModel, relationModel);

            return (model, layer1.ID, layer2.ID, () => new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>()));
        }
    }
}
