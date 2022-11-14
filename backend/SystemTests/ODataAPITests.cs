using FluentAssertions;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.OData.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SystemTests.Base;

namespace SystemTests
{
    [Key("ciid")]
    public class TestTA
    {
        public Guid ciid { get; set; }
        public string name { get; set; }
    }

    public class Container : DataServiceContext
    {
        public Container(Uri serviceRoot) : base(serviceRoot)
        {
            this.testTA = base.CreateQuery<TestTA>("test_trait_as");
        }

        public DataServiceQuery<TestTA> testTA { get; }
    }

    public class ODataAPITests : TestBase
    {
        [Test]
        public async Task TestBasics()
        {
            // create OData context
            var createOdataContxt = new GraphQLRequest
            {
                Query = @"
                    mutation {
	                    upsertSingleByFilter_m__meta__config__odata_context(
                            layers: [""__okconfig""]
                            writeLayer: ""__okconfig""
                            filter: {id: {exact: ""testcontext""}}
		                    input: { id: ""testcontext"", config: """"""{ ""$type"":""ConfigV4"", ""WriteLayerID"":""layer_1"", ""ReadLayerset"":[""layer_1""], ""ContextAuth"":{""type"":""ContextAuthNone""}}""""""}
                    ) {
                        entity {
		                    id
                        }
	                  }
                    }"
            };
            var graphQLResponse = await Query(createOdataContxt, () => new { upsertSingleByFilter_m__meta__config__odata_context = new { entity = new { id = "" } } });
            Assert.IsNull(graphQLResponse.Errors);
            Assert.AreEqual("testcontext", graphQLResponse.Data.upsertSingleByFilter_m__meta__config__odata_context.entity.id);


            // create layer_1
            var createLayer = new GraphQLRequest
            {
                Query = @"mutation {
                manage_createLayer(id: ""layer_1"") {
                    id
                }
            }"
            };
            var r2 = await Query(createLayer, () => new { manage_createLayer = new { id = "" } });
            Assert.IsNull(r2.Errors);


            var createTrait = new GraphQLRequest
            {
                Query = @"
mutation {
  manage_upsertRecursiveTrait(
    trait: {
      id: ""test_trait_a""
      requiredAttributes: [
        {
          identifier: ""name""
          template: {
            name: ""test_trait_a.name""
            type: TEXT
            isID: true
            isArray: false
            valueConstraints: []
          }
        }
      ]
      optionalAttributes: [],
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

            Thread.Sleep(6000); // sleep for a bit to let omnikeeper update its edm model and graphql schema

            // insert an entity
            var insertNew = new GraphQLRequest
            {
                Query = @"
mutation {
  insertNew_test_trait_a(
    layers: [""layer_1""]
    writeLayer: ""layer_1""
    ciName: ""Entity 1""
    input: { name: ""entity_1"" }
  ) {
                entity { name }
  }
        }
"
            };
            var r3 = await Query(insertNew, () => new { insertNew_test_trait_a = new { entity = new { name = "" } } });
            Assert.IsNull(r3.Errors);
            Assert.AreEqual("entity_1", r3.Data.insertNew_test_trait_a.entity.name);

            var serviceRoot = $"{BaseUrl}/api/odata/testcontext";
            var context = new Container(new Uri(serviceRoot));

            context.BuildingRequest += (sender, eventArgs) =>
            {
                // NOTE: for metadata URL, we add an accept header to force XML response, because the client cannot work with a json reponse
                if (eventArgs.RequestUri.Segments.Any(s => s.Equals("$metadata")))
                    eventArgs.Headers.TryAdd("Accept", "application/xml");
            };

            var testTAs = context.testTA.Execute();
            Assert.IsNotNull(testTAs);

            var extracted = testTAs.ToList();
            Assert.AreEqual(1, extracted.Count);
            Assert.AreEqual("entity_1", extracted.First().name);
        }
    }
}
