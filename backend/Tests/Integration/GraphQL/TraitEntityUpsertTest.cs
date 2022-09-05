using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityUpsertTest : QueryTestBase
    {
        [Test]
        public async Task TestUpsertByDataID()
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
        },
        {
          identifier: ""members""
          template: { 
            predicateID: ""is_member_of""
            directionForward: false
            traitHints: []
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


            // initial insert
            var mutationUpsert1 = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", name: ""Entity 1"", assignments: [""" + $"{relatedCIID3}" + @"""] }
  ) {
                entity { id }
  }
        }
";
            var expected2 = @"
{
  ""upsertByDataID_test_trait_a"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
";
            AssertQuerySuccess(mutationUpsert1, expected2, user);


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
                        members { relatedCIID }
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
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": null,
              ""assignments"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }],
              ""members"": []
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected3, user);

            // modify other trait relation with an upsert
            var mutationUpsert2 = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", name: ""Entity 1"", members: [""" + $"{relatedCIID2}" + @""", """ + $"{relatedCIID3}" + @"""] }
  ) {
                entity { id }
  }
        }
";
            AssertQuerySuccess(mutationUpsert2, expected2, user);

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
              }],
              ""members"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID2}" + @"""
              },{
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }]
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);


            // modify first trait relation with an upsert again, removing them
            var mutationUpsert3 = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", name: ""Entity 1"", assignments: [] }
  ) {
                entity { id }
  }
        }
";
            AssertQuerySuccess(mutationUpsert3, expected2, user);

            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""name"": ""Entity 1"",
              ""optional"": null,
              ""assignments"": [],
              ""members"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID2}" + @"""
              },{
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }]
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected5, user);


            // modify both trait relation with an upsert yet again
            var mutationUpsert4 = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", name: ""Entity 1"", assignments: [""" + $"{relatedCIID2}" + @"""], members: [""" + $"{relatedCIID3}" + @"""] }
  ) {
                entity { id }
  }
        }
";
            AssertQuerySuccess(mutationUpsert4, expected2, user);

            var expected6 = @"
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
                    ""relatedCIID"": """ + $"{relatedCIID2}" + @"""
              }],
              ""members"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }]
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected6, user);
        }


        [Test]
        public async Task TestUpsertSingleAndDeletionByFilter()
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
        },
        {
          identifier: ""members""
          template: { 
            predicateID: ""is_member_of""
            directionForward: false
            traitHints: []
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


            // initial insert
            var mutationUpsert1 = @"
mutation {
  upsertSingleByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {exact:""Entity 1""}}
    input: { name: ""Entity 1"", assignments: [""" + $"{relatedCIID3}" + @"""] }
  ) {
                entity { name }
  }
        }
";
            var expected2 = @"
{
  ""upsertSingleByFilter_test_trait_a"": {
	""entity"": {
        ""name"": ""Entity 1""
      }
	}
  }
";
            AssertQuerySuccess(mutationUpsert1, expected2, user);


            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        name
                        optional
                        assignments { relatedCIID }
                        members { relatedCIID }
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
              ""name"": ""Entity 1"",
              ""optional"": null,
              ""assignments"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }],
              ""members"": []
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected3, user);


            // insert a second trait entity
            // initial insert
            var mutationUpsert2 = @"
            mutation {
              upsertSingleByFilter_test_trait_a(
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                filter: {name: {exact:""Entity 2""}}
                input: { name: ""Entity 2"", assignments: [] }
              ) {
                            entity { name }
              }
                    }
            ";
                        var expected4 = @"
            {
              ""upsertSingleByFilter_test_trait_a"": {
	            ""entity"": {
                    ""name"": ""Entity 2""
                  }
	            }
              }
            ";
            AssertQuerySuccess(mutationUpsert2, expected4, user);


            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          {
            ""entity"": {
              ""name"": ""Entity 1"",
              ""optional"": null,
              ""assignments"": [
              {
                    ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
              }],
              ""members"": []
            }
          },
          {
            ""entity"": {
              ""name"": ""Entity 2"",
              ""optional"": null,
              ""assignments"": [],
              ""members"": []
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected5, user);

            // modify trait with an upsert
            var mutationUpsert3 = @"
            mutation {
              upsertSingleByFilter_test_trait_a(
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                filter: {name: {exact:""Entity 1""}}
                input: { name: ""Entity 1 changed"", members: [""" + $"{relatedCIID2}" + @""", """ + $"{relatedCIID3}" + @"""] }
              ) {
                            entity { name }
              }
                    }
            ";
            var expected6 = @"
            {
              ""upsertSingleByFilter_test_trait_a"": {
	            ""entity"": {
                    ""name"": ""Entity 1 changed""
                  }
	            }
              }
            ";
            AssertQuerySuccess(mutationUpsert3, expected6, user);

            var expected7 = @"
            {
              ""traitEntities"": {
            	  ""test_trait_a"": {
            	    ""all"": [
                      {
                        ""entity"": {
                          ""name"": ""Entity 2"",
                          ""optional"": null,
                          ""assignments"": [],
                          ""members"": []
                        }
                      },
                      {
                        ""entity"": {
                          ""name"": ""Entity 1 changed"",
                          ""optional"": null,
                          ""assignments"": [
                          {
                                ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
                          }],
                          ""members"": [
                          {
                                ""relatedCIID"": """ + $"{relatedCIID2}" + @"""
                          },{
                                ""relatedCIID"": """ + $"{relatedCIID3}" + @"""
                          }]
                        }
                      }
                    ]
            	  }
              }
            }
            ";
            AssertQuerySuccess(queryTestTraitA, expected7, user);

            // delete a trait entity by filter
            var mutationDelete1 = @"
            mutation {
              deleteSingleByFilter_test_trait_a(
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                filter: {name: {exact:""Entity 1 changed""}}
              )
            }
            ";
            var expected8 = @"
            {
              ""deleteSingleByFilter_test_trait_a"": true
            }";
            AssertQuerySuccess(mutationDelete1, expected8, user);


            // try to delete again
            var expected9 = @"
            {
              ""deleteSingleByFilter_test_trait_a"": false
            }";
            AssertQuerySuccess(mutationDelete1, expected9, user);

            var expected10 = @"
            {
              ""traitEntities"": {
            	  ""test_trait_a"": {
            	    ""all"": [
                      {
                        ""entity"": {
                          ""name"": ""Entity 2"",
                          ""optional"": null,
                          ""assignments"": [],
                          ""members"": []
                        }
                      }
                    ]
            	  }
              }
            }
            ";
            AssertQuerySuccess(queryTestTraitA, expected10, user);
        }
    }
}
