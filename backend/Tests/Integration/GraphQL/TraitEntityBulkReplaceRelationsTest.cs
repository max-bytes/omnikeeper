using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;
using Tests.Integration.GraphQL.Base;

namespace Tests.Integration.GraphQL
{
    class TraitEntityBulkReplaceRelationsTest : QueryTestBase
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


            // insert base trait entities, without relations
            var initialBulkInsert = @"
mutation {
  bulkReplace_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: [{ciid: ""e4125f12-0257-4835-aa25-b8f83a64a38c"", attributes: {id: 1, name: ""testname_a""}}, 
            {ciid: ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", attributes: {id: 2, name: ""testname_b""}},
            {ciid: ""fb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", attributes: {id: 3, name: ""testname_c""}}]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(initialBulkInsert, @"{ ""bulkReplace_test_trait_a"": { ""isNoOp"": false } }", user);


            var expectedNoop = @"{ ""bulkReplaceRelations_test_trait_a_assignments"": { ""isNoOp"": true } }";
            var expectedOp = @"{ ""bulkReplaceRelations_test_trait_a_assignments"": { ""isNoOp"": false } }";

            // first bulk
            var mutationBulkReplaceRelations1 = @"
mutation {
  bulkReplaceRelations_test_trait_a_assignments(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: [
        {baseCIID: ""e4125f12-0257-4835-aa25-b8f83a64a38c"", relatedCIIDs: [""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c""]},
        {baseCIID: ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", relatedCIIDs: [""e4125f12-0257-4835-aa25-b8f83a64a38c"", ""fb3772f6-6d3e-426b-86f3-1ff8ba165d0c""]}]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplaceRelations1, expectedOp, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplaceRelations1, expectedNoop, user);

            // second bulk, with different relations
            var mutationBulkReplaceRelations2 = @"
mutation {
  bulkReplaceRelations_test_trait_a_assignments(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: [
        {baseCIID: ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", relatedCIIDs: [""e4125f12-0257-4835-aa25-b8f83a64a38c""]},
        {baseCIID: ""fb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", relatedCIIDs: [""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", ""e4125f12-0257-4835-aa25-b8f83a64a38c""]}]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplaceRelations2, expectedOp, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplaceRelations2, expectedNoop, user);

            // fetch final data
            var query = @"
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
            var expectedQuery1 = @"
{
  ""traitEntities"": {
	  ""test_trait_a"": {
	    ""all"": [
          { ""entity"": { ""id"": 1, ""name"": ""testname_a"", ""optional"": null, ""assignments"": [] } },
          { ""entity"": { ""id"": 2, ""name"": ""testname_b"", ""optional"": null, ""assignments"": [{""relatedCIID"": ""e4125f12-0257-4835-aa25-b8f83a64a38c""}] } },
          { ""entity"": { ""id"": 3, ""name"": ""testname_c"", ""optional"": null, ""assignments"": [{""relatedCIID"": ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c""}, {""relatedCIID"": ""e4125f12-0257-4835-aa25-b8f83a64a38c""}] } }
        ]
	  }
  }
}
";
            AssertQuerySuccess(query, expectedQuery1, user);
        }
    }
}
