using GraphQL;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityWithGeneratedAttributeTest : QueryTestBase
    {
        [Test]
        public async Task TestFetching()
        {
            var userInDatabase = await SetupDefaultUser();
            var (_, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("__okconfig", ModelContextBuilder.BuildImmediate());
            var (_, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("layer_1", ModelContextBuilder.BuildImmediate());
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


            // insert generator
            var mutationInsertGenerator = @"
mutation {
  manage_upsertGenerator(
    generator: {
      id: ""test_generator""
      attributeName: ""test_trait_a.optional""
      attributeValueTemplate: ""attributes[\""test_trait_a.id\""] | string.upcase""
    }
  ) {
    id
    }
}
";
            RunQuery(mutationInsertGenerator, user);

            // add generator to layer
            var mutationAddGeneratorToLayer = @"
mutation {
  manage_upsertLayerData(layer: { id: ""layer_1"", generators: [""test_generator""], description: """", color: 0 }) {
    id
  }
    }
";
            RunQuery(mutationAddGeneratorToLayer, user);


            // insert existing entities
            var mutationInsert = @"
mutation($ciName: String!, $id: String!) {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: $ciName
    input: { id: $id }
  ) {
                entity { id }
  }
}
";
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_1" }, { "ciName", "entity_1" } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_2" }, { "ciName", "entity_2" } }));
            RunQuery(mutationInsert, user, new Inputs(new Dictionary<string, object?>() { { "id", "entity_3" }, { "ciName", "entity_3" } }));


            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        optional
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
	    ""all"": [{
            ""entity"": {
              ""id"": ""entity_1"",
              ""optional"": ""ENTITY_1""
            }
          },{
            ""entity"": {
              ""id"": ""entity_2"",
              ""optional"": ""ENTITY_2""
            }
          },{
            ""entity"": {
              ""id"": ""entity_3"",
              ""optional"": ""ENTITY_3""
            }
          }]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected2, user);

            // filtered query, using generated attribute
            var queryFilteredTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                filtered(filter: {optional:{regex:{pattern: ""ENTITY_[23]""}}}) {
                    entity {
                        id
                        optional
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
	    ""filtered"": [{
            ""entity"": {
              ""id"": ""entity_2"",
              ""optional"": ""ENTITY_2""
            }
          },{
            ""entity"": {
              ""id"": ""entity_3"",
              ""optional"": ""ENTITY_3""
            }
          }]
	  }
  }
}
";
            AssertQuerySuccess(queryFilteredTestTraitA, expected3, user);

        }
    }
}
