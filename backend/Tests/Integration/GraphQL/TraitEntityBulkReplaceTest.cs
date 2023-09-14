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

            var expectedNoop = @"{ ""bulkReplace_test_trait_a"": { ""isNoOp"": true } }";
            var expectedOp = @"{ ""bulkReplace_test_trait_a"": { ""isNoOp"": false } }";

            // insert initial set
            var mutationBulkReplace1 = @"
mutation {
  bulkReplace_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    input: [{ciid: ""e4125f12-0257-4835-aa25-b8f83a64a38c"", attributes: {id: 1, name: ""testname_a""}}, {ciid: ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"", attributes: {id: 2, name: ""testname_b""}}]
  ) {
    isNoOp
  }
}
";
            AssertQuerySuccess(mutationBulkReplace1, expectedOp, user);

            // do it again, should return false
            AssertQuerySuccess(mutationBulkReplace1, expectedNoop, user);

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
          { ""entity"": { ""id"": 1, ""name"": ""testname_a"", ""optional"": null } },
          { ""entity"": { ""id"": 2, ""name"": ""testname_b"", ""optional"": null } }
        ]
	  }
  }
}
";
            AssertQuerySuccess(query, expectedQuery1, user);

            // another update
            var mutationBulkReplace3 = @"
            mutation {
              bulkReplace_test_trait_a(
                layers: [""layer_1""]
                writeLayer: ""layer_1""
                input: [{ciid: ""e4125f12-0257-4835-aa25-b8f83a64a38c"", attributes: {id: 1, name: ""testname_a_changed""}}, {ciid: ""1b3772f6-6d3e-426b-86f3-1ff8ba165d0c"", attributes: {id: 3, name: ""testname_new""}}]
              ) {
                isNoOp
                success
                changeset {
                  ciAttributes {
                    ciid
                    attributes {
                      name
                    }
                  }
                  removedCIAttributes {
                    ciid
                    attributes {
                      name
                    }
                  }
                }
              }
            }";
            var expected3 = @"
            {
              ""bulkReplace_test_trait_a"":{
            	 ""isNoOp"":false,
            	 ""success"":true,
            	 ""changeset"":{
            		""ciAttributes"":[
            		   {
                          ""ciid"": ""e4125f12-0257-4835-aa25-b8f83a64a38c"",
            			  ""attributes"":[
            				 {
            					""name"":""test_trait_a.name""
            				 }
            			  ]
            		   },
            		   {
                          ""ciid"": ""1b3772f6-6d3e-426b-86f3-1ff8ba165d0c"",
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
            		   {
                          ""ciid"": ""eb3772f6-6d3e-426b-86f3-1ff8ba165d0c"",
            			  ""attributes"":[
            				 {
            					""name"":""test_trait_a.id""
            				 },
            				 {
            					""name"":""test_trait_a.name""
            				 }
            			  ]
            		   }
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
                      { ""entity"": { ""id"": 1, ""name"": ""testname_a_changed"", ""optional"": null } },
                      { ""entity"": { ""id"": 3, ""name"": ""testname_new"", ""optional"": null } }
                    ]
            	  }
              }
            }
            ";
            AssertQuerySuccess(query, expectedQuery2, user);
        }


    }
}
