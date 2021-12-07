using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.TraitBased;

namespace Tests.Integration.Model
{
    public abstract class GenericTraitEntityModelTestBase<T, ID> : DIServicedTestBase where T : TraitEntity, new() where ID : notnull
    {
        public GenericTraitEntityModelTestBase() : base(true)
        {
        }

        protected async Task TestGenericModelChange(Func<T> creator1, Func<T> creator1Changed, ID id1)
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    (entityOut, _) = await model.InsertOrUpdate(entityIn,
                        layerset, writeLayerID,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            var byDataID1 = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(entity1);
            var getAllByDataID = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataID.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1.entity },
            });


            var entity1Updated = await Insert(creator1Changed());
            var byDataID1Updated = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1Updated.entity.Should().BeEquivalentTo(entity1Updated);
            var getAllByDataIDUpdated = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataIDUpdated.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1Updated.entity },
            });
        }

        protected async Task TestGenericModelGetByDataID(Func<T> creator1, Func<T> creator2, ID id1, ID id2, ID nonExistentID)
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    (entityOut, _) = await model.InsertOrUpdate(entityIn,
                        layerset, writeLayerID,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            var entity2 = await Insert(creator2());

            var byDataID1 = await model.GetSingleByDataID(id1, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(entity1);
            var byDataID2 = await model.GetSingleByDataID(id2, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(entity2);
            var byNonExistentID = await model.GetSingleByDataID(nonExistentID, layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(ValueTuple.Create<T?, Guid>(null, default), byNonExistentID);

            var getAllByDataID = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataID.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1.entity },
                { id2, byDataID2.entity },
            });
        }

        protected async Task TestGenericModelOperations(Func<T> creator1, Func<T> creator2, ID entity1ID, ID entity2ID, ID nonExistantID)
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var rt1 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            async Task<T> Insert(T entityIn, IChangesetProxy changesetProxy, bool expectChange = true)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    bool changed;
                    (entityOut, changed) = await model.InsertOrUpdate(entityIn,
                        layerset, writeLayerID,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy, trans);

                    Assert.IsNotNull(entityOut);
                    Assert.AreEqual(expectChange, changed);

                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1(), changesetBuilder());
            entity1.Should().BeEquivalentTo(creator1());

            var rt2 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt2.Count());
            rt2.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 } }, options => options.WithoutStrictOrdering());

            var entity2 = await Insert(creator2(), changesetBuilder());
            entity2.Should().BeEquivalentTo(creator2());

            var rt3 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt3.Count());
            rt3.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // delete using non existant CIID
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(nonExistantID, layerset, writeLayerID,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                Assert.IsFalse(result);
            }

            // delete one of the existing ones
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(entity1ID, layerset, writeLayerID,
                        new DataOriginV1(DataOriginType.Manual), changesetBuilder(), trans);
                Assert.IsTrue(result);

                trans.Commit();
            }

            var rt4 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt4.Count());
            rt4.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // re-insert
            var entity1Re = await Insert(creator1(), changesetBuilder());
            entity1Re.Should().BeEquivalentTo(creator1());

            var rt5 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt5.Count());
            rt5.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());


            // try to insert same again
            var entity1Re2 = await Insert(creator1(), changesetBuilder(), false);
            entity1Re2.Should().BeEquivalentTo(creator1());

            var rt6 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt6.Count());
            rt6.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

        }


        protected async Task TestGenericModelBulkReplace(Func<T> creator1, Func<T> creator2, Func<T> creator2Changed, ID entity1ID, ID entity2ID)
        {
            var (model, layerset, writeLayerID, changesetBuilder) = await SetupModel();

            var rt1 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            var entity1 = creator1();
            var entity2 = creator2();
            var entity2Changed = creator2Changed();


            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var ciidsAtStart = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());

            // initial insert
            // +2 CIs
            var c1 = await model.BulkReplace(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(c1);
            var rt2 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt2.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // same operation again, nothing must change
            var c2 = await model.BulkReplace(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsFalse(c2);
            var rt3 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt3.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // remove one entity
            var c3 = await model.BulkReplace(new Dictionary<ID, T>() { { entity2ID, entity2 } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(c3);
            var rt4 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt4.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());

            // completely different set
            // +1 CIs
            var c4 = await model.BulkReplace(new Dictionary<ID, T>() { { entity1ID, entity1 } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(c4);
            var rt5 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt5.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 } }, options => options.WithoutStrictOrdering());

            // add missing entity again
            // +1 CIs
            var c5 = await model.BulkReplace(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(c5);
            var rt6 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt6.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2 } }, options => options.WithoutStrictOrdering());


            // change one entity
            var c6 = await model.BulkReplace(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2Changed } }, layerset, writeLayerID, new DataOriginV1(DataOriginType.Manual), changesetBuilder(), ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(c6);
            var rt7 = await model.GetAllByDataID(layerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt7.Should().BeEquivalentTo(new Dictionary<ID, T>() { { entity1ID, entity1 }, { entity2ID, entity2Changed } }, options => options.WithoutStrictOrdering());


            // make sure of the correct amount of created CIs
            var ciids = await ciModel.GetCIIDs(ModelContextBuilder.BuildImmediate());
            Assert.AreEqual(4, ciids.Count() - ciidsAtStart.Count());
        }

        protected async Task<(GenericTraitEntityModel<T, ID> model, LayerSet layerset, string writeLayerID, Func<IChangesetProxy> changesetBuilder)> SetupModel()
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var layer = await ServiceProvider.GetRequiredService<ILayerModel>().UpsertLayer("testlayer", ModelContextBuilder.BuildImmediate());
            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[] { new AuthRole("ar1", new string[] {
                    PermissionUtils.GetLayerReadPermission(layer), PermissionUtils.GetLayerWritePermission(layer),
                })
                });
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var relationModel = ServiceProvider.GetRequiredService<IRelationModel>();

            var model = new GenericTraitEntityModel<T, ID>(effectiveTraitModel, ciModel, attributeModel, relationModel);

            return (model, new LayerSet(layer.ID), layer.ID, () => new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>()));
        }
    }
}
