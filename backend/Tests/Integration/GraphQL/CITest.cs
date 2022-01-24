using GraphQL;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class CITest : QueryTestBase
    {
        [Test]
        public async Task TestBasicQuery()
        {
            var username = "testUser";
            var userGUID = new Guid("7dc848b7-881d-4785-9f25-985e9b6f2715");
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await ciModel.CreateCI(trans);
            var userInDatabase = await userModel.UpsertUser(username, username, userGUID, UserType.Robot, trans);
            var changeset = new ChangesetProxy(userInDatabase, TimeThreshold.BuildLatest(), changesetModel);
            var (layer1, _) = await layerModel.CreateLayerIfNotExists("layer_1", trans);
            var (layer2, _) = await layerModel.CreateLayerIfNotExists("layer_2", trans);
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[]
                {
                    new AuthRole("ar1", new string[] { PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1) }),
                    new AuthRole("ar2", new string[] { PermissionUtils.GetLayerReadPermission(layer2), PermissionUtils.GetLayerWritePermission(layer2) }),
                });
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueInteger(3), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            trans.Commit();

            string query = @"
                    query($ciids: [Guid]!, $layers: [String]!) {
                      cis(ciids: $ciids, layers: $layers) {
                        mergedAttributes {
                            attribute {
                                name
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
                    { "ciids", new Guid[] { ciid1 } },
                    { "layers", new string[] { "layer_1", "layer_2" } }
                });

            var expected = @"{
                      ""cis"":[
                          {
                             ""mergedAttributes"":[
                                {
                                    ""attribute"": {
                                       ""name"":""a1"",
                                       ""value"":{
                                          ""type"":""INTEGER"",
                                          ""isArray"": false,
                                          ""values"":[""3""]
                                       }
                                    }
                                }
                             ]
                          }
                        ]
                    }";

            var httpContext = new DefaultHttpContext();
            AssertQuerySuccess(query, expected, inputs, userContext: new OmnikeeperUserContext(user, ServiceProvider));
        }
    }
}
