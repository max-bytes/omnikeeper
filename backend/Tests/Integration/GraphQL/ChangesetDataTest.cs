using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class ChangesetDataTest : QueryTestBase
    {
        [Test]
        public async Task TestBasicChangesetData()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var userInDatabase = await SetupDefaultUser();
            var changesetProxy = await CreateChangesetProxy();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", trans);
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[]
                {
                    new AuthRole("ar1", new string[] { PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1) }),
                });
            trans.Commit();

            await ReinitSchema();

            string query = @"
                mutation($read_layers: [String]!, $write_layer: String!, $ciid: Guid!) {
                    A : mutateCIs(writeLayer: $write_layer, readLayers: $read_layers, insertAttributes: [
                    {
                        ci: $ciid,
                        name: ""foo"",
                        value:
                        {
                            type: TEXT,
                            isArray: false,
                            values: [""bar1""]
                        }
                    }
                    ]) {
                        affectedCIs {
                            id
                        }
                    }
                    B : insertChangesetData(layer: $write_layer, attributes: [
                    {
                        name: ""foo2"",
                        value:
                        {
                            type: TEXT,
                            isArray: false,
                            values: [""bar2""]
                        }
                    }
                    ]) {
                        changesetDataCIID
                    }
                }";

            var inputs = new Inputs(new Dictionary<string, object?>()
                {
                    { "ciid", ciid1 },
                    { "read_layers", new string[] { "layer_1" } },
                    { "write_layer", "layer_1" }
                });

            var expected = JsonDocument.Parse(@$"{{""affectedCIs"":[{{""id"":""{ciid1}""}}]}}");

            var (queryResult, resultJson) = RunQuery(query, user, inputs);
            var d = JsonDocument.Parse(resultJson);
            var aResult = d.RootElement.GetProperty("data").GetProperty("A");
            var elementComparer = new JsonElementComparer();
            Assert.IsTrue(elementComparer.Equals(aResult, expected.RootElement));

            var changesetDataCIID = d.RootElement.GetProperty("data").GetProperty("B").GetProperty("changesetDataCIID").GetGuid();

            // assert that only a single changeset was created
            using var transI = ModelContextBuilder.BuildImmediate();
            var numChangesets = await GetService<IChangesetModel>().GetNumberOfChangesets(transI);
            Assert.AreEqual(1, numChangesets);

            // fetch changeset and its associated data
            var changeset = await GetService<IChangesetModel>().GetLatestChangesetForLayer("layer_1", transI, TimeThreshold.BuildLatest());
            Assert.IsNotNull(changeset);
            string query2 = @"
                query($layers: [String]!, $changeset_id: Guid!) {
                    changeset(layers: $layers, id: $changeset_id) {
                        dataCIID
                        data {
                          mergedAttributes {
                            attribute {
                              name
                              value {
                                value
                              }
                            }
                          }
                        }
                      }
                    }
            ";
            var inputs2 = new Inputs(new Dictionary<string, object?>()
                {
                    { "changeset_id", changeset!.ID },
                    { "layers", new string[] { "layer_1" } },
                });


            var expected2 = @$"{{
                ""changeset"": {{
                  ""dataCIID"": ""{changesetDataCIID}"",
                  ""data"": {{
	                ""mergedAttributes"": [
	                  {{
		                ""attribute"": {{
		                  ""name"": ""changeset_data.changeset_id"",
		                  ""value"": {{
			                ""value"": ""{changeset!.ID}""
		                  }}
		                }}
	                  }},
                      {{
		                ""attribute"": {{
		                  ""name"": ""__name"",
		                  ""value"": {{
			                ""value"": ""Changeset-Data - {changeset!.ID}""
		                  }}
		                }}
	                  }},
                      {{
		                ""attribute"": {{
		                  ""name"": ""foo2"",
		                  ""value"": {{
			                ""value"": ""bar2""
		                  }}
		                }}
	                  }}
	                ]
                  }}
                }}
            }}";

            AssertQuerySuccess(query2, expected2, user, inputs2);
        }
    }
}
