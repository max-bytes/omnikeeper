using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityBulkReplaceTest : QueryTestBase
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
          identifier: ""id""
          template: {
            name: ""test_trait_a.id""
            type: INTEGER
            isID: false
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

            // insert unrelated entity
            var mutationBulkReplace0 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""unrelated.*""}}}
    input: [{id: 0, name: ""unrelated_a""}],
    idAttributes: [""id""]
  )
}
";
            var expectedTrue = @"{ ""bulkReplaceByFilter_test_trait_a"": true }";
            var expectedFalse = @"{ ""bulkReplaceByFilter_test_trait_a"": false }";
            AssertQuerySuccess(mutationBulkReplace0, expectedTrue, user);

            // insert initial set
            var mutationBulkReplace1 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname.*""}}}
    input: [{id: 1, name: ""testname_a""}, {id: 2, name: ""testname_b""}],
    idAttributes: [""id""]
  )
}
";
            AssertQuerySuccess(mutationBulkReplace1, expectedTrue, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplace1, expectedFalse, user);

            // update 1
            var mutationBulkReplace2 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname.*""}}}
    input: [{id: 1, name: ""testname_a""}, {id: 2, name: ""testname_b""}, {id: 3, name: ""testname_c""}],
    idAttributes: [""id""]
  )
}
";
            AssertQuerySuccess(mutationBulkReplace2, expectedTrue, user);

            var query = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
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
            var expectedQuery1 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          { ""entity"": { ""id"": 0, ""name"": ""unrelated_a"", ""optional"": null } },
          { ""entity"": { ""id"": 1, ""name"": ""testname_a"", ""optional"": null } },
          { ""entity"": { ""id"": 2, ""name"": ""testname_b"", ""optional"": null } },
          { ""entity"": { ""id"": 3, ""name"": ""testname_c"", ""optional"": null } }
        ]
	  }
  }
}
";
            AssertQuerySuccess(query, expectedQuery1, user);

            // another update
            var mutationBulkReplace3 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname_[ab]""}}}
    input: [{id: 1, name: ""testname_a2""}, {id: 2, name: ""testname_b"", optional: ""foo""}, {id: 4, name: ""testname_d""}],
    idAttributes: [""id""]
  )
}
";
            AssertQuerySuccess(mutationBulkReplace3, expectedTrue, user);

            var expectedQuery2 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          { ""entity"": { ""id"": 0, ""name"": ""unrelated_a"", ""optional"": null } },
          { ""entity"": { ""id"": 1, ""name"": ""testname_a2"", ""optional"": null } },
          { ""entity"": { ""id"": 2, ""name"": ""testname_b"", ""optional"": ""foo"" } },
          { ""entity"": { ""id"": 3, ""name"": ""testname_c"", ""optional"": null } },
          { ""entity"": { ""id"": 4, ""name"": ""testname_d"", ""optional"": null } }
        ]
	  }
  }
}
";
            AssertQuerySuccess(query, expectedQuery2, user);

            // another update
            var mutationBulkReplace4 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname_.*""}}}
    input: [{id: 4, name: ""testname_d""}],
    idAttributes: [""id""]
  )
}
";
            AssertQuerySuccess(mutationBulkReplace4, expectedTrue, user);

            var expectedQuery3 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          { ""entity"": { ""id"": 0, ""name"": ""unrelated_a"", ""optional"": null } },
          { ""entity"": { ""id"": 4, ""name"": ""testname_d"", ""optional"": null } }
        ]
	  }
  }
}
";
            AssertQuerySuccess(query, expectedQuery3, user);
        }
    }
}
