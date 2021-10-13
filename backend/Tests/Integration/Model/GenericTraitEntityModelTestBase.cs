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

namespace Tests.Integration.Model
{
    abstract class GenericTraitEntityModelTestBase : DIServicedTestBase
    {
        public GenericTraitEntityModelTestBase() : base(true)
        {
        }

        protected async Task TestGenericModelGetByDataID<T, ID>(Func<T> creator1, Func<T> creator2, ID id1, ID id2, ID nonExistentID) where T : TraitEntity, new() where ID : notnull
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var layerOKConfig = await ServiceProvider.GetRequiredService<ILayerModel>().UpsertLayer("__okconfig", ModelContextBuilder.BuildImmediate());
            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new HashSet<string>() {
                    PermissionUtils.GetLayerReadPermission(layerOKConfig), PermissionUtils.GetLayerWritePermission(layerOKConfig),
                });
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var baseRelationModel = ServiceProvider.GetRequiredService<IBaseRelationModel>();
            var metaConfigurationModel = ServiceProvider.GetRequiredService<IMetaConfigurationModel>();

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(ModelContextBuilder.BuildImmediate());

            var model = new GenericTraitEntityModel<T, ID>(effectiveTraitModel, ciModel, attributeModel, baseRelationModel);

            var changesetProxy1 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    (entityOut, _) = await model.InsertOrUpdate(entityIn,
                        metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);
                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            var entity2 = await Insert(creator2());

            var byDataID1 = await model.GetSingleByDataID(id1, metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID1.entity.Should().BeEquivalentTo(entity1);
            var byDataID2 = await model.GetSingleByDataID(id2, metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            byDataID2.entity.Should().BeEquivalentTo(entity2);
            var byNonExistentID = await model.GetSingleByDataID(nonExistentID, metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(ValueTuple.Create<T?, Guid>(null, default), byNonExistentID);

            var getAllByDataID = await model.GetAllByDataID(metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            getAllByDataID.Should().BeEquivalentTo(new Dictionary<ID, T>()
            {
                { id1, byDataID1.entity },
                { id2, byDataID2.entity },
            });

        }

        protected async Task TestGenericModelOperations<T, ID>(Func<T> creator1, Func<T> creator2, ID entity1ID, ID nonExistantID) where T : TraitEntity, new() where ID : notnull
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var layerOKConfig = await ServiceProvider.GetRequiredService<ILayerModel>().UpsertLayer("__okconfig", ModelContextBuilder.BuildImmediate());
            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new HashSet<string>() {
                    PermissionUtils.GetLayerReadPermission(layerOKConfig), PermissionUtils.GetLayerWritePermission(layerOKConfig),
                });
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var baseRelationModel = ServiceProvider.GetRequiredService<IBaseRelationModel>();
            var metaConfigurationModel = ServiceProvider.GetRequiredService<IMetaConfigurationModel>();

            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(ModelContextBuilder.BuildImmediate());

            var model = new GenericTraitEntityModel<T, ID>(effectiveTraitModel, ciModel, attributeModel, baseRelationModel);

            var rt1 = await model.GetAll(metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            var changesetProxy1 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            async Task<T> Insert(T entityIn)
            {
                T entityOut;
                using (var trans = ModelContextBuilder.BuildDeferred())
                {
                    bool changed;
                    (entityOut, changed) = await model.InsertOrUpdate(entityIn,
                        metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);

                    Assert.IsNotNull(entityOut);
                    Assert.IsTrue(changed);

                    trans.Commit();
                }
                return entityOut;
            }

            var entity1 = await Insert(creator1());
            entity1.Should().BeEquivalentTo(creator1());

            var rt2 = await model.GetAll(metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt2.Count());
            rt2.Select(t => t.entity).Should().BeEquivalentTo(new List<T>() { entity1 }, options => options.WithoutStrictOrdering());

            var entity2 = await Insert(creator2());
            entity2.Should().BeEquivalentTo(creator2());

            var rt3 = await model.GetAll(metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(2, rt3.Count());
            rt3.Select(t => t.entity).Should().BeEquivalentTo(new List<T>() { entity1, entity2 }, options => options.WithoutStrictOrdering());

            // delete using non existant CIID
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(nonExistantID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);
                Assert.IsFalse(result);
            }

            // delete one of the existing ones
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var result = await model.TryToDelete(entity1ID, metaConfiguration.ConfigLayerset, metaConfiguration.ConfigWriteLayer,
                        new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);
                Assert.IsTrue(result);

                trans.Commit();
            }

            var rt4 = await model.GetAll(metaConfiguration.ConfigLayerset, ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, rt4.Count());
            rt4.Select(t => t.entity).Should().BeEquivalentTo(new List<T>() { entity2 }, options => options.WithoutStrictOrdering());
        }
    }
}
