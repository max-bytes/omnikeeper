using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityFilteringTest : QueryTestBase
    {
        [Test]
        public async Task TestFiltering()
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
mutation($name: String!, $id: String!) {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: $name
    input: { id: $id, name: $name }
  ) {
                entity { id }
  }
        }
";

            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_1" }, { "name", "Entity 1" } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_2" }, { "name", "Entity 2" } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_3" }, { "name", "Entity 3" } }));

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
	    ""all"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""name"": ""Entity 1""
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2""
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_3"",
              ""name"": ""Entity 3""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);


            var queryFiltered = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {name: {regex:{pattern: ""Entity [23]""}}}) {
                    entity {
                        id
                        name
                    }
                }
            }
        }
    }
";
            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2""
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_3"",
              ""name"": ""Entity 3""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered, expected5, user);


            var queryFiltered2 = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {id: {exact: ""entity_2""}, name: {regex:{pattern: ""Entity [23]""}}}) {
                    entity {
                        id
                        name
                    }
                }
            }
        }
    }
";
            var expected6 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered2, expected6, user);
        }


        [Test]
        public async Task TestBooleanFiltering()
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
      ]
      optionalAttributes: [
        {
          identifier: ""flag""
          template: {
            name: ""test_trait_a.flag""
            type: BOOLEAN
            isID: false
            isArray: false
            valueConstraints: []
          }
        }]
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
mutation($flag: Boolean, $id: String!) {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: $id
    input: { id: $id, flag: $flag }
  ) {
                entity { id }
  }
        }
";

            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_1" }, { "flag", true } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_2" }, { "flag", false } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_3" }, { "flag", null } }));

            // there must not be an entity
            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        flag
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
	    ""all"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""flag"": true
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""flag"": false
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_3"",
              ""flag"": null
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);


            var queryFiltered = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {flag: {isTrue: true}}) {
                    entity {
                        id
                        flag
                    }
                }
            }
        }
    }
";
            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_1"",
              ""flag"": true
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered, expected5, user);
        }


        [Test]
        public async Task TestAdvancedFiltering()
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
      optionalAttributes: [
        {
          identifier: ""optional""
          template: {
            name: ""test_trait_a.optional""
            type: TEXT
            isID: false
            isArray: false
            valueConstraints: []
          }
        }
      ]
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
mutation($name: String!, $id: String!, $optional: String) {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: $name
    input: { id: $id, name: $name, optional: $optional }
  ) {
                entity { id }
  }
        }
";

            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_1" }, { "name", "Entity 1" }, { "optional", null } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_2" }, { "name", "Entity 2" }, { "optional", "set" } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_3" }, { "name", "Entity 3" }, { "optional", null } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_4" }, { "name", "Entity 4" }, { "optional", "set" } }));


            var queryFiltered = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {name: {regex:{pattern: ""Entity [23]""}}, id: {exact: ""entity_2""}}) {
                    entity {
                        id
                        name
                        optional
                    }
                }
            }
        }
    }
";
            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2"",
              ""optional"": ""set""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered, expected5, user);


            var queryFiltered2 = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {name: {regex:{pattern: ""Entity [34]""}}, optional: {isSet: false}}) {
                    entity {
                        id
                        name
                        optional
                    }
                }
            }
        }
    }
";
            var expected6 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_3"",
              ""name"": ""Entity 3"",
              ""optional"": null
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered2, expected6, user);

            var queryFiltered3 = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {name: {regex:{pattern: ""Entity [234]""}}, optional: {isSet: true}}) {
                    entity {
                        id
                        name
                        optional
                    }
                }
            }
        }
    }
";
            var expected7 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2"",
              ""optional"": ""set""
            }
          },
          {
            ""entity"": {
              ""id"": ""entity_4"",
              ""name"": ""Entity 4"",
              ""optional"": ""set""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered3, expected7, user);
        }


        [Test]
        public async Task TestRelationFiltering()
        {
            var userInDatabase = await SetupDefaultUser();
            var (layerOkConfig, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
            var user = new AuthenticatedInternalUser(userInDatabase);

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

            var mutationInsert = @"
mutation($name: String!, $id: String!, $assignments: [Guid]!) {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: $name
    input: { id: $id, name: $name, assignments: $assignments }
  ) {
                entity { id }
  }
        }
";

            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_1" }, { "name", "Entity 1" }, { "assignments", new Guid[] { } } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_2" }, { "name", "Entity 2" }, { "assignments", new Guid[] { relatedCIID1 } } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_3" }, { "name", "Entity 3" }, { "assignments", new Guid[] { relatedCIID1, relatedCIID2 } } }));

            var queryFiltered = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {assignments: {exactOtherCIID: """ + $"{relatedCIID1}" + @"""}}) {
                    entity {
                        id
                        name
                    }
                }
            }
        }
    }
";
            var expected5 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""filtered"": [
          {
            ""entity"": {
              ""id"": ""entity_2"",
              ""name"": ""Entity 2""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryFiltered, expected5, user);
        }
    }
}
