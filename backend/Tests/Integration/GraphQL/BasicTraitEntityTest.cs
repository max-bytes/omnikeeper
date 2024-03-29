﻿using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class BasicTraitEntityTest : QueryTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var userInDatabase = await SetupDefaultUser();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedInternalUser(userInDatabase);

            // create CIs to relate to
            var relatedCIID1 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var relatedCIID2 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var relatedCIID3 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());

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
            AssertQuerySuccess(mutationCreateTrait, expected1, user);

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
                latestChangeAll {
                    userID
                    layerID
                }
            }
        }
    }
";
            var expected2 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [],
        ""latestChangeAll"": null
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected2, user);

            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: ""entity_1"", name: ""Entity 1"", assignments: [""" + $"{relatedCIID3}" + @"""] }
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
";
            AssertQuerySuccess(mutationInsert, expected3, user);


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
              ""assignments"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }]
            }
          }
        ],
        ""latestChangeAll"": {
            ""userID"": " + userInDatabase.ID + @",
            ""layerID"": ""layer_1""
        }
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);



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
";
            AssertQuerySuccess(mutationUpdateAttribute, expected5, user);

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
              ""assignments"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }]
            }
          }
        ],
        ""latestChangeAll"": {
            ""userID"": " + userInDatabase.ID + @",
            ""layerID"": ""layer_1""
        }
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected6, user);

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
            var (_, jsonStr) = RunQuery(queryCIID, user);
            var json = JsonDocument.Parse(jsonStr);

            var ciidEntity1 = json.RootElement.GetProperty("data").GetProperty("traitEntities").GetProperty("test_trait_a").GetProperty("byDataID").GetProperty("ciid").GetGuid();

            var mutationSetAssignments = @"
mutation($baseCIID: Guid!, $relatedCIIDs: [Guid]!) {
  setRelationsByCIID_test_trait_a_assignments (
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    baseCIID: $baseCIID
    relatedCIIDs: $relatedCIIDs
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
";
            AssertQuerySuccess(mutationSetAssignments, expected7, user,
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
        ],
        ""latestChangeAll"": {{
            ""userID"": " + userInDatabase.ID + $@",
            ""layerID"": ""layer_1""
        }}
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected8, user);


            var expected9 = @"
{
  ""setRelationsByCIID_test_trait_a_assignments"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
";
            AssertQuerySuccess(mutationSetAssignments, expected9, user,
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
        ],
        ""latestChangeAll"": {{
            ""userID"": " + userInDatabase.ID + $@",
            ""layerID"": ""layer_1""
        }}
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected10, user);

            // add assignemnts
            var mutationAddAssignments = @"
mutation($baseCIID: Guid!, $relatedCIIDsToAdd: [Guid]!) {
  addRelationsByCIID_test_trait_a_assignments (
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    baseCIID: $baseCIID
    relatedCIIDsToAdd: $relatedCIIDsToAdd
  ) {
                entity { id }
  }
        }
";
            var expected11 = @"
{
  ""addRelationsByCIID_test_trait_a_assignments"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
";
            AssertQuerySuccess(mutationAddAssignments, expected11, user,
                new Inputs(new Dictionary<string, object?>()
                {
                    { "baseCIID", ciidEntity1 },
                    { "relatedCIIDsToAdd", new Guid[] { relatedCIID2, relatedCIID3 } }
                }));

            var expected12 = $@"
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
              }},
              {{
                ""relatedCIID"": ""{relatedCIID3}""
              }}]
            }}
          }}
        ],
        ""latestChangeAll"": {{
            ""userID"": " + userInDatabase.ID + $@",
            ""layerID"": ""layer_1""
        }}
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected12, user);

            // remove assignemnts
            var mutationRemoveAssignments = @"
mutation($baseCIID: Guid!, $relatedCIIDsToRemove: [Guid]!) {
  removeRelationsByCIID_test_trait_a_assignments (
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    baseCIID: $baseCIID
    relatedCIIDsToRemove: $relatedCIIDsToRemove
  ) {
                entity { id }
  }
        }
";
            var expected13 = @"
{
  ""removeRelationsByCIID_test_trait_a_assignments"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
";
            AssertQuerySuccess(mutationRemoveAssignments, expected13, user,
                new Inputs(new Dictionary<string, object?>()
                {
                    { "baseCIID", ciidEntity1 },
                    { "relatedCIIDsToRemove", new Guid[] { relatedCIID1, relatedCIID2 } }
                }));

            var expected14 = $@"
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
                ""relatedCIID"": ""{relatedCIID3}""
              }}]
            }}
          }}
        ],
        ""latestChangeAll"": {{
            ""userID"": " + userInDatabase.ID + $@",
            ""layerID"": ""layer_1""
        }}
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected14, user);

            // delete entity
            var mutationDeleteEntity = @"
mutation($ciid: Guid!) {
  deleteByCIID_test_trait_a (
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciid: $ciid
  )
        }
";
            var expected15 = @"
{
  ""deleteByCIID_test_trait_a"": true
}";
            AssertQuerySuccess(mutationDeleteEntity, expected15, user, new Inputs(new Dictionary<string, object?>()
                {
                    { "ciid", ciidEntity1 }
                }));

            var expected16 = $@"
{{
  ""traitEntities"": {{
	  ""test_trait_a"": {{
	    ""all"": [],
        ""latestChangeAll"": {{
            ""userID"": " + userInDatabase.ID + $@",
            ""layerID"": ""layer_1""
        }}
	  }}
  }}
}}
";
            AssertQuerySuccess(queryTestTraitA, expected16, user);
        }

        [Test]
        public async Task TestIncorrectInsert()
        {
            var userInDatabase = await SetupDefaultUser();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedInternalUser(userInDatabase);

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
            valueConstraints: [
                """"""{""$type"":""Omnikeeper.Base.Entity.CIAttributeValueConstraintTextLength, Omnikeeper.Base"",""Minimum"":1,""Maximum"":null}""""""
            ]
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

            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: """", name: ""Entity 1"" }
  ) {
                entity { id }
  }
        }
";

            AssertQueryHasErrors(mutationInsert, user);

            // there must not be an entity
            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        name
                    }
                }
            }
        }
    }
";
            var expected4 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": []
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);


            // no CI must be created
            var isLayerEmpty = await GetService<ILayerStatisticsModel>().IsLayerEmpty(layer1.ID, ModelContextBuilder.BuildImmediate());
            Assert.IsTrue(isLayerEmpty);
        }
    }
}
