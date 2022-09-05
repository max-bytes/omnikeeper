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
            var user = new AuthenticatedInternalUser(userInDatabase);
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
            var changeset = await GetService<IChangesetModel>().GetLatestChangeset(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, null, new string[] { "layer_1" }, transI, TimeThreshold.BuildLatest());
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

        [Test]
        public async Task TestTraitEntityChangesetData()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var userInDatabase = await SetupDefaultUser();
            var changesetProxy = await CreateChangesetProxy();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", trans);
            var user = new AuthenticatedInternalUser(userInDatabase);
            trans.Commit();

            await ReinitSchema();

            string mutationCreateTrait = @"
mutation {
  manage_upsertRecursiveTrait(
    trait: {
      id: ""test_trait_a""
      requiredAttributes: [
        {
          identifier: ""name""
          template: {
            name: ""test_trait_a.name""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          }
        }
      ]
      optionalAttributes: []
      optionalRelations: [],
      requiredTraits: []
    }
  ) {
    id
  }
}
";
            var expected1 = @"
{
    ""manage_upsertRecursiveTrait"":
        {
            ""id"": ""test_trait_a""
        }
}";
            AssertQuerySuccess(mutationCreateTrait, expected1, user);

            // force rebuild graphql schema
            await ReinitSchema();

            // insert some data + changeset data as trait entity
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
                    B : insertChangesetData_test_trait_a(layer: $write_layer, input: 
                    {
                        name: ""bar2""
                    }) {
                        latestChange {
                            id
                        }
                    }
                }";

            var inputs = new Inputs(new Dictionary<string, object?>()
                {
                    { "ciid", ciid1 },
                    { "read_layers", new string[] { "layer_1" } },
                    { "write_layer", "layer_1" }
                });


            var (queryResult, resultJson) = RunQuery(query, user, inputs);
            var d = JsonDocument.Parse(resultJson);
            var aResult = d.RootElement.GetProperty("data").GetProperty("A");
            var bResult = d.RootElement.GetProperty("data").GetProperty("B");
            var elementComparer = new JsonElementComparer();
            var expectedA = JsonDocument.Parse(@$"{{""affectedCIs"":[{{""id"":""{ciid1}""}}]}}");
            Assert.IsTrue(elementComparer.Equals(aResult, expectedA.RootElement));

            // assert that only a single changeset was created
            using var transI = ModelContextBuilder.BuildImmediate();
            var numChangesets = await GetService<IChangesetModel>().GetNumberOfChangesets(layer1.ID, transI);
            Assert.AreEqual(1, numChangesets);

            // fetch changeset and its associated data
            var changeset = await GetService<IChangesetModel>().GetLatestChangeset(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, null, new string[] { layer1.ID }, transI, TimeThreshold.BuildLatest());
            Assert.IsNotNull(changeset);

            // check that latestChange of mutation corresponds to the changeset
            var expectedB = JsonDocument.Parse(@$"{{""latestChange"":{{""id"":""{changeset!.ID}""}}}}");
            Assert.IsTrue(elementComparer.Equals(bResult, expectedB.RootElement));

            string query2 = @"
                query($layers: [String]!, $changeset_id: Guid!) {
                    changeset(layers: $layers, id: $changeset_id) {
                        data {
                          traitEntity {
                            test_trait_a {
                              entity {
                                name
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
                  ""data"": {{
	                ""traitEntity"":
	                  {{
                        ""test_trait_a"": {{
                          ""entity"": {{
                            ""name"": ""bar2""
                          }}
                        }}
	                  }}
                  }}
                }}
            }}";

            AssertQuerySuccess(query2, expected2, user, inputs2);
        }
    }
}
