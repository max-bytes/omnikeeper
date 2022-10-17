using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityUpdateTest : QueryTestBase
    {
        [Test]
        public async Task TestUpdateByCIIDAndFilter()
        {
            var userInDatabase = await SetupDefaultUser();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedInternalUser(userInDatabase);

            // create CIs to relate to
            //var relatedCIID1 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            //var relatedCIID2 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            //var relatedCIID3 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());

            // force rebuild graphql schema
            await ReinitSchema();

            string mutationCreateTrait = @"
mutation {
  manage_upsertRecursiveTrait(
    trait: {
      id: ""test_trait_a""
      requiredAttributes: [
        {
          identifier: ""a1""
          template: {
            name: ""test_trait_a.a1""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          }
        },
        {
          identifier: ""a2""
          template: {
            name: ""test_trait_a.a2""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          }
        }
      ]
      optionalAttributes: [
        {
          identifier: ""a3""
          template: {
            name: ""test_trait_a.a3""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          }
        },
        {
          identifier: ""a4""
          template: {
            name: ""test_trait_a.a4""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          },
        }
        {
          identifier: ""a5""
          template: {
            name: ""test_trait_a.a5""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          },
        }
        {
          identifier: ""a6""
          template: {
            name: ""test_trait_a.a6""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          },
        }
      ]
      optionalRelations: [
      ],
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


            // initial insert
            var mutationInsert1 = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { a1: ""v1"", a2: ""v2"", a3: ""v3"", a4: ""v4"" }
  ) {
                ciid
  }
        }
";
            var (_, writtenResult) = RunQuery(mutationInsert1, user);
            var ciid1 = JsonDocument.Parse(writtenResult).RootElement.GetProperty("data").GetProperty("insertNew_test_trait_a").GetProperty("ciid").GetGuid();

            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        a1, a2, a3, a4, a5, a6
                    }
                }
            }
        }
    }
";

            var expected3 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          {
            ""entity"": {
                ""a1"": ""v1"",
                ""a2"": ""v2"",
                ""a3"": ""v3"",
                ""a4"": ""v4"",
                ""a5"": null,
                ""a6"": null
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected3, user);


            // modify trait with an update by ciid
            var mutationUpdate1 = $@"
            mutation {{
              updateByCIID_test_trait_a(
                ciid: ""{ciid1}""
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                input: {{ a1: ""v1n"", a3: ""v3n"", a4: null, a5: ""v5n"" }}
              ) {{
                            entity {{ a1, a2, a3, a4, a5, a6 }}
              }}
                    }}
            ";
            var expected4 = @"
            {
              ""updateByCIID_test_trait_a"": {
	            ""entity"": {
                    ""a1"": ""v1n"",
                    ""a2"": ""v2"",
                    ""a3"": ""v3n"",
                    ""a4"": null,
                    ""a5"": ""v5n"",
                    ""a6"": null
                  }
	            }
              }
            ";
            AssertQuerySuccess(mutationUpdate1, expected4, user);


            // modify trait again with an update by filter
            var mutationUpdate2 = $@"
            mutation {{
              updateSingleByFilter_test_trait_a(
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                filter: {{ a1: {{exact: ""v1n"" }} }}
                input: {{ a2: ""v2n"", a3: null }}
              ) {{
                            entity {{ a1, a2, a3, a4, a5, a6 }}
              }}
                    }}
            ";
            var expected5 = @"
            {
              ""updateSingleByFilter_test_trait_a"": {
	            ""entity"": {
                    ""a1"": ""v1n"",
                    ""a2"": ""v2n"",
                    ""a3"": null,
                    ""a4"": null,
                    ""a5"": ""v5n"",
                    ""a6"": null
                  }
	            }
              }
            ";
            AssertQuerySuccess(mutationUpdate2, expected5, user);
        }
    }
}
