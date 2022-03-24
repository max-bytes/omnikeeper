using GraphQL;
using GraphQL.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityTest : QueryTestBase
    {
        [Test]
        public async Task TestBasicQuery()
        {
            var userInDatabase = await SetupDefaultUser();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedUser(userInDatabase,
                new AuthRole[]
                {
                    new AuthRole("ar1", new string[] { 
                        PermissionUtils.GetLayerReadPermission(layer1), PermissionUtils.GetLayerWritePermission(layer1), 
                        PermissionUtils.GetLayerReadPermission(layerOkConfig), PermissionUtils.GetLayerWritePermission(layerOkConfig), 
                        PermissionUtils.GetManagementPermission() 
                    }),
                });

            // force rebuild graphql schema
            await ReinitSchema();

            string mutationCreateTrait = @"
mutation {
  manage_upsertRecursiveTrait(
    trait: {
      id: ""test_trait_a""
      requiredAttributes: [
        {
          identifier: ""id""
          template: {
            name: ""test_trait_a.id""
            type: TEXT
            isID: true
            isArray: false
            valueConstraints: []
          }
        }
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
      optionalAttributes: [
        {
          identifier: ""optional""
          template: {
            name: ""test_trait_a.optional""
            type: INTEGER
            isID: false
            isArray: false
            valueConstraints: []
          }
        }
      ]
      optionalRelations: [
        {
          identifier: ""assignments""
          template: { 
            predicateID: ""is_assigned_to""
            directionForward: true
            traitHints: [""test_trait_a""]
          }
        }
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
            AssertQuerySuccess(mutationCreateTrait, expected1, new OmnikeeperUserContext(user, ServiceProvider));

            // force rebuild graphql schema
            await ReinitSchema();


            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        name
                        optional
                        assignments { relatedCIID }
                    }
                }
            }
        }
    }
";
            var expected2 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": []
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected2, new OmnikeeperUserContext(user, ServiceProvider));

            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: ""entity_1"", name: ""Entity 1"" }
  ) {
                entity { id }
  }
        }
";

            var expected3 = @"
{
  ""insertNew_test_trait_a"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
}
";
            AssertQuerySuccess(mutationInsert, expected3, new OmnikeeperUserContext(user, ServiceProvider));


            var expected4 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": null,
              ""assignments"": []
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, new OmnikeeperUserContext(user, ServiceProvider));



            var mutationUpdateAttribute = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", name: ""Entity 1"", optional: 3 }
  ) {
                entity { id }
  }
        }
";
            var expected5 = @"
{
  ""upsertByDataID_test_trait_a"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
}
";
            AssertQuerySuccess(mutationUpdateAttribute, expected5, new OmnikeeperUserContext(user, ServiceProvider));

            var expected6 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": 3,
              ""assignments"": []
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected6, new OmnikeeperUserContext(user, ServiceProvider));

            // update relations
            var queryCIID = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                byDataID(id: {id: ""entity_1""}) {
                    ciid
                }
            }
        }
    }
";
            var (_, jsonStr) = RunQuery(queryCIID, new OmnikeeperUserContext(user, ServiceProvider));
            var json = JToken.Parse(jsonStr);

            var ciidEntity1Str = json["data"]!["traitEntities"]!["test_trait_a"]!["byDataID"]!["ciid"]!.Value<string>();
            var ciidEntity1 = Guid.Parse(ciidEntity1Str);

            // create CIs to relate to
            var relatedCIID1 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var relatedCIID2 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var mutationUpdateAssignments = @"
mutation($baseCIID: Guid!, $relatedCIIDs: [Guid]!) {
  setRelationsByCIID_test_trait_a_assignments (
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { baseCIID: $baseCIID, relatedCIIDs: $relatedCIIDs }
  ) {
                entity { id }
  }
        }
";
            var expected7 = @"
{
  ""setRelationsByCIID_test_trait_a_assignments"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
}
";
            AssertQuerySuccess(mutationUpdateAssignments, expected7, new OmnikeeperUserContext(user, ServiceProvider),
                new Inputs(new Dictionary<string, object?>()
                {
                    { "baseCIID", ciidEntity1 },
                    { "relatedCIIDs", new Guid[] { relatedCIID1, relatedCIID2 } }
                }));

            var expected8 = $@"
{{
  ""traitEntities"": {{
	  ""test_trait_a"": {{
	    ""all"": [
          {{
            ""entity"": {{
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": 3,
              ""assignments"": [
              {{
                ""relatedCIID"": ""{relatedCIID1}""
              }},
              {{
                ""relatedCIID"": ""{relatedCIID2}""
              }}]
            }}
          }}
        ]
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected8, new OmnikeeperUserContext(user, ServiceProvider));


            var expected9 = @"
{
  ""setRelationsByCIID_test_trait_a_assignments"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
}
";
            AssertQuerySuccess(mutationUpdateAssignments, expected9, new OmnikeeperUserContext(user, ServiceProvider),
                new Inputs(new Dictionary<string, object?>()
                {
                    { "baseCIID", ciidEntity1 },
                    { "relatedCIIDs", new Guid[] { relatedCIID2 } }
                }));


            var expected10 = $@"
{{
  ""traitEntities"": {{
	  ""test_trait_a"": {{
	    ""all"": [
          {{
            ""entity"": {{
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": 3,
              ""assignments"": [
              {{
                ""relatedCIID"": ""{relatedCIID2}""
              }}]
            }}
          }}
        ]
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected10, new OmnikeeperUserContext(user, ServiceProvider));
        }
    }
}
