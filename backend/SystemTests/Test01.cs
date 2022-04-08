using FluentAssertions;
using GraphQL;
using GraphQL.Client.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using SystemTests.Base;

namespace SystemTests
{
    public class Test01 : GraphQLTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var ciidsRequest = new GraphQLRequest
            {
                Query = @"
                {
                    ciids
                }"
            };
            var graphQLResponse = await Query(ciidsRequest, () => new { ciids = new List<Guid>() });

            Assert.IsNull(graphQLResponse.Errors);
            Assert.AreEqual(0, graphQLResponse.Data.ciids.Count);
        }


        [Test]
        public async Task TestTraitEntities()
        {
            var createTrait = new GraphQLRequest
            {
                Query = @"
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
        }
      ],
      requiredTraits: []
    }
  ) {
    id
  }
}
"
        };
            var r1 = await Query(createTrait, () => new { manage_upsertRecursiveTrait = new { id = "" } });
            Assert.IsNull(r1.Errors);
            Assert.AreEqual("test_trait_a", r1.Data.manage_upsertRecursiveTrait.id);

            // create layer_1
            var createLayer = new GraphQLRequest
            {
                Query = @"mutation {
                manage_createLayer(id: ""layer_1"") {
                    id
                }
            }"}; 
            var r2 = await Query(createLayer, () => new { manage_createLayer = new { id = "" } });
            Assert.IsNull(r2.Errors);


            var insertNew = new GraphQLRequest
            {
                Query = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { id: ""entity_1"", name: ""Entity 1"" }
  ) {
                entity { id }
  }
        }
"
            };
            var r3 = await Query(insertNew, () => new { insertNew_test_trait_a = new { entity = new { id = "" } } });
            Assert.IsNull(r3.Errors);
            Assert.AreEqual("entity_1", r3.Data.insertNew_test_trait_a.entity.id);


            var fetchAll = new GraphQLRequest
            {
                Query = @"
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
"
            };
            var r4 = await Query(fetchAll, () => new { traitEntities = new { test_trait_a = new { all = new List<EntityWrapperType>() } } });
            Assert.IsNull(r4.Errors);
            r4.Data.traitEntities.test_trait_a.all.Should().BeEquivalentTo(new EntityWrapperType[]
            {
                new EntityWrapperType{ Entity = new EntityType() { Id = "entity_1", Name = "Entity 1", Assignments = ImmutableList<RelatedAssignmentsType>.Empty}}
            });

        }

        class EntityWrapperType
        {
            public EntityType Entity { get;set; }
        }
        class EntityType
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public long? Optional { get; set; }
            public IList<RelatedAssignmentsType> Assignments { get; set; }
        }

        class RelatedAssignmentsType
        {
            public Guid RelatedCIID { get; set; }
        }
    }
}
