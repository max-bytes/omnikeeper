using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
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
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var userInDatabase = await SetupDefaultUser();
            var changeset = await CreateChangesetProxy();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", trans);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_2", trans);
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[]
                {
                    new AuthRole("ar1", new string[] { PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1) }),
                    new AuthRole("ar2", new string[] { PermissionUtils.GetLayerReadPermission(layer2), PermissionUtils.GetLayerWritePermission(layer2) }),
                });
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueInteger(3), ciid1, layer1.ID, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
            trans.Commit();

            await ReinitSchema();

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
                                    values
                                }
                            }
                        }
                    }
                }
                ";

            var inputs = new Inputs(new Dictionary<string, object?>()
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

            AssertQuerySuccess(query, expected, user, inputs);
        }
    }
}
