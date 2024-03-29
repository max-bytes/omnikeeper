﻿using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityWithJsonTest : QueryTestBase
    {
        [Test]
        public async Task Test()
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
        {
          identifier: ""json""
          template: {
            name: ""test_trait_a.json""
            type: JSON
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


            var queryTestTraitA = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        json
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

            var mutationInsert = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: ""entity_1"", json: ""{\""foo\"":    \""bar\""}"" }
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
              ""json"": ""{\""foo\"":    \""bar\""}""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected4, user);

            // test that inserting the same json produces no change, and no changeset
            var mutationUpdateAttribute = @"
mutation {
  upsertByDataID_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: { id: ""entity_1"", json: ""{\""foo\"":    \""bar\""}"" }
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
              ""json"": ""{\""foo\"":    \""bar\""}""
            }
          }
        ]
	  }
  }
}
";
            AssertQuerySuccess(queryTestTraitA, expected6, user);

            var numChanges = await GetService<IChangesetModel>().GetNumberOfChangesets("layer_1", ModelContextBuilder.BuildImmediate());
            Assert.AreEqual(1, numChanges);
        }
    }
}
