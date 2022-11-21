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
  ) {
    isNoOp
  }
}
";
            var expectedNoop = @"{ ""bulkReplaceByFilter_test_trait_a"": { ""isNoOp"": true } }";
            var expectedOp = @"{ ""bulkReplaceByFilter_test_trait_a"": { ""isNoOp"": false } }";
            AssertQuerySuccess(mutationBulkReplace0, expectedOp, user);

            // insert initial set
            var mutationBulkReplace1 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname.*""}}}
    input: [{id: 1, name: ""testname_a""}, {id: 2, name: ""testname_b""}],
    idAttributes: [""id""]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplace1, expectedOp, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplace1, expectedNoop, user);

            // update 1
            var mutationBulkReplace2 = @"
mutation {
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {name: {regex: {pattern: ""testname.*""}}}
    input: [{id: 1, name: ""testname_a""}, {id: 2, name: ""testname_b""}, {id: 3, name: ""testname_c""}],
    idAttributes: [""id""]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplace2, expectedOp, user);

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
  ) {
    isNoOp
    success
    changeset {
      ciAttributes {
        attributes {
          name
        }
      }
      removedCIAttributes {
        attributes {
          name
        }
      }
    }
  }
}
";
            var expected3 = @"
{
  ""bulkReplaceByFilter_test_trait_a"":{
	 ""isNoOp"":false,
	 ""success"":true,
	 ""changeset"":{
		""ciAttributes"":[
		   {
			  ""attributes"":[
				 {
					""name"":""test_trait_a.name""
				 }
			  ]
		   },
		   {
			  ""attributes"":[
				 {
					""name"":""test_trait_a.optional""
				 }
			  ]
		   },
		   {
			  ""attributes"":[
				 {
					""name"":""test_trait_a.id""
				 },
				 {
					""name"":""test_trait_a.name""
				 }
			  ]
		   }
		],
		""removedCIAttributes"":[
		   
		]
	 }
  }
}";
            AssertQuerySuccess(mutationBulkReplace3, expected3, user);

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
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplace4, expectedOp, user);

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


        [Test]
        public async Task TestTraitRelationID()
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
      optionalAttributes: []
      optionalRelations: [{
          identifier: ""assignments""
          template: { 
            predicateID: ""is_assigned_to""
            directionForward: true
            traitHints: []
          }
        }],
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

            // add other CIs for relations
            var relatedCIID1 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var relatedCIID2 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());
            var relatedCIID3 = await GetService<ICIModel>().CreateCI(ModelContextBuilder.BuildImmediate());

            var expectedNoop = @"{ ""bulkReplaceByFilter_test_trait_a"": { ""isNoOp"": true } }";
            var expectedOp = @"{ ""bulkReplaceByFilter_test_trait_a"": { ""isNoOp"": false } }";

            // insert initial set
            var mutationBulkReplace1 = @$"
mutation {{
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {{name: {{regex: {{pattern: ""testname.*""}}}}}}
    input: [{{id: 1, name: ""testname_a"", assignments: [""{relatedCIID1}""]}}, {{id: 2, name: ""testname_b"", assignments: [""{relatedCIID1}"", ""{relatedCIID2}""]}}, {{id: 3, name: ""testname_c"", assignments: [""{relatedCIID2}""]}}],
    idAttributes: [""id""]
    idRelations: [""assignments""]
  ) {{
    isNoOp
  }}
}}
";
            AssertQuerySuccess(mutationBulkReplace1, expectedOp, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplace1, expectedNoop, user);

            // update 1
            var mutationBulkReplace2 = @$"
mutation {{
  bulkReplaceByFilter_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    filter: {{name: {{regex: {{pattern: ""testname.*""}}}}}}
    input: [{{id: 1, name: ""testname_a2"", assignments: [""{relatedCIID1}""]}}, {{id: 2, name: ""testname_b2"", assignments: [""{relatedCIID1}"", ""{relatedCIID2}""]}}, {{id: 4, name: ""testname_d"", assignments: [""{relatedCIID3}""]}}],
    idAttributes: [""id""]
    idRelations: [""assignments""]
  ) {{
    isNoOp
  }}
}}
";
            AssertQuerySuccess(mutationBulkReplace2, expectedOp, user);

            var query = @"
{
  traitEntities(layers: [""layer_1""]) {
    test_trait_a {
                all {
                    entity {
                        id
                        name
                        assignments {
                            relatedCIID
                        }
                    }
                }
            }
        }
    }
            ";
            var expectedQuery1 = @$"
{{
  ""traitEntities"": {{
	  ""test_trait_a"": {{
	    ""all"": [
          {{ ""entity"": {{ ""id"": 1, ""name"": ""testname_a2"", ""assignments"": [{{ ""relatedCIID"": ""{relatedCIID1}""}}] }} }},
          {{ ""entity"": {{ ""id"": 2, ""name"": ""testname_b2"", ""assignments"": [{{ ""relatedCIID"": ""{relatedCIID1}""}}, {{ ""relatedCIID"": ""{relatedCIID2}""}}] }} }},
          {{ ""entity"": {{ ""id"": 4, ""name"": ""testname_d"", ""assignments"": [{{ ""relatedCIID"": ""{relatedCIID3}""}}] }} }}
        ]
	  }}
  }}
}}
";
            AssertQuerySuccess(query, expectedQuery1, user);
        }
    }
}
