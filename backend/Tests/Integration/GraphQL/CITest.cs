﻿
using GraphQL;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.GraphQL;
using LandscapeRegistry.Model;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.Integration.GraphQL
{
    class CITest : QueryTestBase<RegistrySchema>
    {
        public CITest()
        {
            DBSetup.Setup();

            Services.Register<ICISearchModel, CISearchModel>();
            Services.Register<ICIModel, CIModel>();
            Services.Register<IAttributeModel, AttributeModel>();
            Services.Register<IBaseAttributeModel, BaseAttributeModel>();
            Services.Register<IUserInDatabaseModel, UserInDatabaseModel>();
            Services.Register<ILayerModel, LayerModel>();
            Services.Register<IRelationModel, RelationModel>();
            Services.Register<IBaseRelationModel, BaseRelationModel>();
            Services.Register<IChangesetModel, ChangesetModel>();
            Services.Register<ITemplateModel, TemplateModel>();
            Services.Register<IPredicateModel, PredicateModel>();
            Services.Register<IMemoryCacheModel>(() => null);
            Services.Register<ITraitModel, TraitModel>();
            Services.Register<IOIAConfigModel, OIAConfigModel>();

            Services.Register<ITraitsProvider, TraitsProvider>();
            Services.Register<ITemplatesProvider, TemplatesProvider>();

            Services.Register<ILogger<TraitModel>>(() => NullLogger<TraitModel>.Instance);
            Services.Register<ILogger<OIAConfigModel>>(() => NullLogger<OIAConfigModel>.Instance);

            var authorizationService = new Mock<IRegistryAuthorizationService>();
            Services.Register<IRegistryAuthorizationService>(() => authorizationService.Object);

            var currentUserService = new Mock<ICurrentUserService>();
            Services.Register<ICurrentUserService>(() => currentUserService.Object);

            Services.Register<RegistryQuery>();
            Services.Register<MergedCIType>();
            Services.Singleton(() =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build(DBSetup.dbName, false, true);
            });

            var sp = new SimpleContainerAdapter(Services);
            Services.Singleton<IServiceProvider>(sp);
            Services.Singleton(new RegistrySchema(sp));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Services.Dispose();
        }

        [Test]
        public async Task Test()
        {
            var username = "testUser";
            var userGUID = new Guid("7dc848b7-881d-4785-9f25-985e9b6f2715");
            var ciModel = Services.Get<CIModel>();
            var attributeModel = Services.Get<BaseAttributeModel>();
            var layerModel = Services.Get<LayerModel>();
            var changesetModel = Services.Get<ChangesetModel>();
            var userModel = Services.Get<UserInDatabaseModel>();
            using var trans = Services.Get<NpgsqlConnection>().BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("layer_1", trans);
            var layerID2 = await layerModel.CreateLayer("layer_2", trans);
            var user = AuthenticatedUser.Build(await userModel.UpsertUser(username, username, userGUID, UserType.Robot, trans), new List<Layer>());
            var changeset = ChangesetProxy.Build(user.InDatabase, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeValueIntegerScalar.Build(3), layer1.ID, ciid1, changeset, trans);
            trans.Commit();

            string query = @"
                query text($identity: Guid!, $layers: [String]!) {
                  ci(identity: $identity, layers: $layers) {
                    mergedAttributes {
                        attribute {
                            name
                            state
                            value {
                                type
                                isArray
                                values
                            }
                        }
                    }
                }
            }
            ";

            var inputs = new Inputs(new Dictionary<string, object>()
            {
                { "identity", ciid1 },
                { "layers", new string[] { "layer_1", "layer_2" } }
            });

            var expected = @"{
                  ""ci"":{
                     ""mergedAttributes"":[
                        {
                            ""attribute"": {
                               ""name"":""a1"",
                               ""state"":""NEW"",
                               ""value"":{
                                  ""type"":""INTEGER"",
                                  ""isArray"": false,
                                  ""values"":[""3""]
                               }
                            }
                        }
                     ]
                  }
                }";

            var httpContext = new DefaultHttpContext();
            AssertQuerySuccess(query, expected, inputs, userContext: new RegistryUserContext(user));
        }
    }
}
