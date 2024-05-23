using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityWithDotAttributeIdentifierTest : QueryTestBase
    {
        [Test]
        public async Task TestBasics()
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
          identifier: ""test.id""
          template: {
            name: ""test_trait_a.id""
            type: TEXT
            isID: true
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

            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        test__id
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
            AssertQuerySuccess(queryTestTraitA, expected2, user);

            // NOTE the double underscore intstead of a dot in the query
            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { test__id: ""entity_1"" }
  ) {
                entity { test__id }
  }
        }
";

            var expected3 = @"
{
  ""insertNew_test_trait_a"": {
	""entity"": {
        ""test__id"": ""entity_1""
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
              ""test__id"": ""entity_1""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);
        }
    }
}
