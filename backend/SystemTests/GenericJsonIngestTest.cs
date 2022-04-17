using FluentAssertions;
using GraphQL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using SystemTests.Base;

namespace SystemTests
{
    public class GenericJsonIngestTest : TestBase
    {
        [Test]
        public async Task TestBasics()
        {
            // create layer_1
            var createLayer1 = new GraphQLRequest
            {
                Query = @"mutation {
                manage_createLayer(id: ""layer_1"") {
                    id
                }
            }"
            };
            var r2 = await Query(createLayer1, () => new { manage_createLayer = new { id = "" } });
            Assert.IsNull(r2.Errors);

            // create ingest context
            var insertUrl = $"{BaseUrl}/api/v1/ingest/genericJSON/manage/context";
            var expression = @"[?document=='data.json'] | [].{cis: data.hosts[].[{tempID: ciid(hostname), idMethod: idMethodByData(['hostname']), attributes: [
                    attribute('hostname', hostname),
                    attribute('os', os),
                    attribute('additionals', additionals, 'JSON')
                ]}] | [], relations: `[]`} | [0]";
            var postDataIngestContext = $@"
{{
   ""id"":""test_generic_json_ingest"",
   ""extractConfig"":{{
      ""$type"":""OKPluginGenericJSONIngest.Extract.ExtractConfigPassiveRESTFiles, OKPluginGenericJSONIngest""
   }},
   ""transformConfig"":{{
      ""$type"":""OKPluginGenericJSONIngest.Transform.JMESPath.TransformConfigJMESPath, OKPluginGenericJSONIngest"",
      ""expression"":""{expression}""
   }},
   ""loadConfig"":{{
      ""$type"":""OKPluginGenericJSONIngest.Load.LoadConfig, OKPluginGenericJSONIngest"",
      ""searchLayerIDs"":[
         ""layer_1""
      ],
      ""writeLayerID"":""layer_1""
   }}
}}
            ";
            var httpClient = new HttpClient();
            var content = new StringContent(postDataIngestContext, Encoding.UTF8, "application/json");
            var responseCreateContext = await httpClient.PostAsync(insertUrl, content);
            Assert.AreEqual(HttpStatusCode.OK, responseCreateContext.StatusCode);

            // prepare ingest data and perform ingest
            var ingestData = @"
{
   ""hosts"":[
      {
         ""hostname"":""host_a"",
         ""os"":""windows""
      },
      {
         ""hostname"":""host_b"",
         ""os"":""linux"",
         ""additionals"": {""foo"":""bar""}
      },
      {
         ""hostname"":""host_c""
      }
   ]
}
            ";
            var ingestUrl = $"{BaseUrl}/api/v1/ingest/genericJSON/files?context=test_generic_json_ingest"; 
            var fileContent = new StringContent(ingestData, Encoding.UTF8, "application/json");
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var multipartFormContent = new MultipartFormDataContent();
            multipartFormContent.Add(fileContent, name: "files", fileName: "data.json");
            var responseIngest = await httpClient.PostAsync(ingestUrl, multipartFormContent);
            Assert.AreEqual(HttpStatusCode.OK, responseIngest.StatusCode);

            // check results
            var query = @"
query {
  cis(layers: [""layer_1""], withoutEffectiveTraits: [""empty""]) {
    mergedAttributes{
                attribute{
                    name
                    value{
                        values
                    }
                }
            }
        }
    }";
            var graphQLResponse = await Query(query, () => new { cis = new List<ResultCI>() });

            Assert.IsNull(graphQLResponse.Errors);
            Assert.AreEqual(3, graphQLResponse.Data.cis.Count);

            graphQLResponse.Data.cis.Should().BeEquivalentTo(
                new List<ResultCI>() {
                    new ResultCI() { MergedAttributes = new() {
                        new() { Attribute = new() { Name = "hostname", Value = new() { Values = new List<string>() { "host_a" } }  } },
                        new() { Attribute = new() { Name = "os", Value = new() { Values = new List<string>() { "windows" } }  } }
                    } },
                    new ResultCI() { MergedAttributes = new() {
                        new() { Attribute = new() { Name = "hostname", Value = new() { Values = new List<string>() { "host_b" } }  } },
                        new() { Attribute = new() { Name = "os", Value = new() { Values = new List<string>() { "linux" } }  } },
                        new() { Attribute = new() { Name = "additionals", Value = new() { Values = new List<string>() { "{" + "\n" + "  \"foo\": \"bar\"" + "\n" + "}" } }  } }

                    } },
                    new ResultCI() { MergedAttributes = new() {
                        new() { Attribute = new() { Name = "hostname", Value = new() { Values = new List<string>() { "host_c" } }  } }
                    } },
                }, options => options.WithoutStrictOrdering());
        }
    }

    class ResultCI
    {
        public List<ResultMergedAttribute> MergedAttributes { get; set; }
    }

    class ResultMergedAttribute
    {
        public ResultAttribute Attribute { get; set; }
    }

    class ResultAttribute
    {
        public string Name { get; set; }
        public ResultAttributeValue Value { get; set; }
    }

    class ResultAttributeValue
    {
        public List<string> Values { get; set; }
    }
}
