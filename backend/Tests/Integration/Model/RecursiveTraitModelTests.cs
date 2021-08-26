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
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class RecursiveTraitModelTests : DIServicedTestBase
    {
        public RecursiveTraitModelTests() : base(true)
        {
        }

        [Test]
        public async Task TestBasic()
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var layerOKConfig = await ServiceProvider.GetRequiredService<ILayerModel>().UpsertLayer("__okconfig", ModelContextBuilder.BuildImmediate());
            var userInDatabase = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new HashSet<string>() {
                    PermissionUtils.GetLayerReadPermission(layerOKConfig), PermissionUtils.GetLayerWritePermission(layerOKConfig),
                });
            currentUserServiceMock.Setup(_ => _.GetCurrentUser(It.IsAny<IModelContext>())).ReturnsAsync(user);

            var baseConfigurationModel = ServiceProvider.GetRequiredService<IBaseConfigurationModel>();

            var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(ModelContextBuilder.BuildImmediate());

            var recursiveTraitModel = ServiceProvider.GetRequiredService<IRecursiveDataTraitModel>();

            var rt1 = await recursiveTraitModel.GetRecursiveTraits(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            var changesetProxy1 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            RecursiveTrait trait;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool changed;
                (trait, changed) = await recursiveTraitModel.InsertOrUpdate("test_trait",
                    new List<TraitAttribute>() { new TraitAttribute("test_ta", CIAttributeTemplate.BuildFromParams("test_a", Omnikeeper.Entity.AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { },
                    new List<TraitRelation>() { },
                    new List<string>() { },
                    new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                    new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);

                Assert.IsNotNull(trait);
                Assert.IsTrue(changed);

                trans.Commit();
            }

            var rt2 = await recursiveTraitModel.GetRecursiveTraits(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt2.Should().BeEquivalentTo(new List<RecursiveTrait>() { trait });

            var changesetProxy2 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool deleted = await recursiveTraitModel.TryToDelete(trait.ID,
                    new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer, 
                    new DataOriginV1(DataOriginType.Manual), changesetProxy2, trans);
                Assert.IsTrue(deleted);
                trans.Commit();
            }

            var rt3 = await recursiveTraitModel.GetRecursiveTraits(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt3);
        }
    }
}
