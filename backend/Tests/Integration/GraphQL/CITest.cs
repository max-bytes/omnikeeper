using GraphQL;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.GraphQL;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Microsoft.AspNetCore.Http;
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

            Services.Register<RegistryQuery>();
            Services.Register<MergedCIType>();
            Services.Register<ICIModel, CIModel>();
            Services.Register<ICISearchModel, CISearchModel>();
            Services.Register<AttributeModel>();
            Services.Register<IAttributeModel, AttributeModel>();
            Services.Register<UserInDatabaseModel>();
            Services.Register<LayerModel>();
            Services.Register<ILayerModel, LayerModel>();
            Services.Register<RelationModel>();
            Services.Register<IPredicateModel, PredicateModel>();
            Services.Register<ITemplatesProvider, CachedTemplatesProvider>();
            Services.Register<TemplatesProvider>();
            Services.Register<ITraitsProvider, CachedTraitsProvider>();
            Services.Register<TraitsProvider>();
            Services.Register<RelationType>();
            Services.Register<ChangesetModel>();
            Services.Singleton(() =>
            {
                var dbcb = new DBConnectionBuilder();
                return dbcb.Build(DBSetup.dbName, false);
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
            var attributeModel = Services.Get<AttributeModel>();
            var layerModel = Services.Get<LayerModel>();
            var changesetModel = Services.Get<ChangesetModel>();
            var userModel = Services.Get<UserInDatabaseModel>();
            using var trans = Services.Get<NpgsqlConnection>().BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("layer_1", trans);
            //var layerID2 = await layerModel.CreateLayer("layer_2", trans);
            var user = User.Build(await userModel.UpsertUser(username, userGUID, UserType.Robot, trans), new List<Layer>());
            var changeset = await changesetModel.CreateChangeset(user.InDatabase.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueIntegerScalar.Build(3), layer1.ID, ciid1, changeset, trans);
            trans.Commit();

            string query = @"
                query text($ciIdentity: String!, $layers: [String]!) {
                  ci(identity: $ciIdentity, layers: $layers) {
                    attributes {
                        name
                        state
                        layerID
                        value {
                            __typename
                            ... on AttributeValueIntegerType {
                                valueInteger: value
                            }
                            ... on AttributeValueTextType {
                                valueText: value
                            }
                        }
                    }
                }
            }
            ";

            var inputs = new Inputs(new Dictionary<string, object>()
            {
                { "ciIdentity", "H123" },
                { "layers", new string[] { "layer_1", "layer_2" } }
            });

            var expected = @"{
                  ""ci"":{
                     ""attributes"":[
                        {
                           ""name"":""a1"",
                           ""state"":""NEW"",
                           ""layerID"":1,
                           ""value"":{
                              ""__typename"":""AttributeValueIntegerType"",
                              ""valueInteger"":3
                           }
                        }
                     ]
                  }
                }";

            var httpContext = new DefaultHttpContext();// new HttpContext(new HttpRequest(null, "http://tempuri.org", null), new HttpResponse(null));
            AssertQuerySuccess(query, expected, inputs, userContext: new RegistryUserContext(user));
        }
    }
}
