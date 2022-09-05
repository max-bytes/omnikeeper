using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class OptionalAttributesTraitEntityTest : QueryTestBase
    {
        [Test]
        public async Task TestOptionalAttributes()
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
	    ""all"": []
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected2, user);

            // insert entity, set optional attribute to null
            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: ""entity_1"", optional: null }
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
	    ""all"": [{
            ""entity"": {
              ""id"": ""entity_1"",
              ""optional"": null
            }
          }]
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
    input: { id: ""entity_1"" }
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

            var mutationUpdateAttributeAgain = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", optional: ""foo"" }
  ) {
                entity { id }
  }
        }
";
            var expected6 = @"
{
  ""upsertByDataID_test_trait_a"": {
	""entity"": {
        ""id"": ""entity_1""
      }
	}
  }
";
            AssertQuerySuccess(mutationUpdateAttributeAgain, expected6, user);

            var expected7 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [{
            ""entity"": {
              ""id"": ""entity_1"",
              ""optional"": ""foo""
            }
          }]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected7, user);
        }
    }
}
