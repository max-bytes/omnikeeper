﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration.Service
{
    class RecursiveTraitWriteServiceTests : DIServicedTestBase
    {
        public RecursiveTraitWriteServiceTests() : base(true)
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

            var recursiveTraitWriteService = ServiceProvider.GetRequiredService<IRecursiveTraitWriteService>();
            var recursiveTraitModel = ServiceProvider.GetRequiredService<IRecursiveDataTraitModel>();

            var rt1 = await recursiveTraitModel.GetRecursiveTraits(ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt1);

            var changesetProxy = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), ServiceProvider.GetRequiredService<IChangesetModel>());

            RecursiveTrait trait;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool changed;
                (trait, changed) = await recursiveTraitWriteService.InsertOrUpdate("test_trait",
                    new List<TraitAttribute>() { new TraitAttribute("test_ta", CIAttributeTemplate.BuildFromParams("test_a", Omnikeeper.Entity.AttributeValues.AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))) },
                    new List<TraitAttribute>() { },
                    new List<TraitRelation>() { },
                    new List<string>() { },
                    new DataOriginV1(DataOriginType.Manual), changesetProxy, user, trans);

                Assert.IsNotNull(trait);
                Assert.IsTrue(changed);

                trans.Commit();
            }

            var rt2 = await recursiveTraitModel.GetRecursiveTraits(ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            rt2.Should().BeEquivalentTo(new List<RecursiveTrait>() { trait });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                bool deleted = await recursiveTraitWriteService.TryToDelete(trait.ID, new DataOriginV1(DataOriginType.Manual), changesetProxy, user, trans);
                Assert.IsTrue(deleted);
                trans.Commit();
            }

            var rt3 = await recursiveTraitModel.GetRecursiveTraits(ModelContextBuilder.BuildImmediate(), TimeThreshold.BuildLatest());
            Assert.IsEmpty(rt3);
        }
    }
}