using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using OKPluginValidation.Validation;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;

namespace OKPluginValidation.Tests
{
    class ValidationIssueModelTests : DIServicedTestBase
    {
        public ValidationIssueModelTests() : base(true)
        {
        }

        protected override void InitServices(IServiceCollection services)
        {
            base.InitServices(services);

            // register plugin services
            var plugin = new PluginRegistration();
            plugin.RegisterServices(services);
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

            var validationIssueModel = ServiceProvider.GetRequiredService<IValidationIssueModel>();

            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();

            // create a test ci to relate to
            Guid otherCIID;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                otherCIID = await ciModel.CreateCI(trans);
                trans.Commit();
            }

            var vi1 = await validationIssueModel.GetValidationIssues(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(vi1);

            var changesetProxy1 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            ValidationIssue validationIssue;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool changed;
                (validationIssue, changed) = await validationIssueModel.InsertOrUpdate("test-ID", "test-message", new Guid[] { otherCIID },
                    new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer,
                    new DataOriginV1(DataOriginType.Manual), changesetProxy1, trans);

                Assert.IsNotNull(validationIssue);
                Assert.IsTrue(changed);

                trans.Commit();
            }

            var vi2 = await validationIssueModel.GetValidationIssues(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.AreEqual(1, vi2.Count());
            var tmp = vi2.First().Value;
            validationIssue.Should().BeEquivalentTo(tmp);

            var changesetProxy2 = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool deleted = await validationIssueModel.TryToDelete(validationIssue.ID,
                    new LayerSet(baseConfiguration.ConfigLayerset), baseConfiguration.ConfigWriteLayer, 
                    new DataOriginV1(DataOriginType.Manual), changesetProxy2, trans);
                Assert.IsTrue(deleted);
                trans.Commit();
            }

            var vi3 = await validationIssueModel.GetValidationIssues(new LayerSet(baseConfiguration.ConfigLayerset), ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(vi3);
        }
    }
}
