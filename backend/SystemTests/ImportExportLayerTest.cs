using FluentAssertions;
using GraphQL;
using GraphQL.Client.Abstractions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SystemTests.Base;

namespace SystemTests
{
    public class ImportExportLayerTest : TestBase
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

            // create layer_2
            var createLayer2 = new GraphQLRequest
            {
                Query = @"mutation {
                manage_createLayer(id: ""layer_2"") {
                    id
                }
            }"
            };
            var r3 = await Query(createLayer2, () => new { manage_createLayer = new { id = "" } });
            Assert.IsNull(r3.Errors);

            var createCIsMutation = new GraphQLRequest
            {
                Query = @"
                mutation {
                  createCIs(cis: [{name: ""CI1"", layerIDForName: ""layer_1""}, {name: ""CI2"", layerIDForName: ""layer_1""}]) {
                    ciids
                  }
                }"
            };
            var graphQLResponse1 = await Query(createCIsMutation, () => new { createCIs = new { ciids = new List<Guid>() } });
            Assert.IsNull(graphQLResponse1.Errors);
            Assert.AreEqual(2, graphQLResponse1.Data.createCIs.ciids.Count);

            var ciids = graphQLResponse1.Data.createCIs.ciids;

            var addAttributeMutation = new GraphQLRequest
            {
                Query = @"mutation($ciid: Guid!) {
                mutateCIs(
                  writeLayer: ""layer_1""
                  readLayers: [""layer_1""]
                  insertAttributes: [{ ci: $ciid, name: ""a1"", value: { type: JSON, isArray: true, values: [""""""{ ""foo"": ""bar""}"""""", """"""{ ""foo2"": ""bar""}""""""]} }]) {
                    affectedCIs {
                      id
                    }
                  }
                }",
                Variables = new Dictionary<string, object>
                {
                    { "ciid", ciids[1] }
                }
            };
            var graphQLResponse2 = await Query(addAttributeMutation, () => new { mutateCIs = new { affectedCIs = new List<object> { new { id = new List<Guid>() } } } });
            Assert.IsNull(graphQLResponse2.Errors);

            // export
            var httpClient = new HttpClient();
            var exportUrl = $"{BaseUrl}/api/v1/ImportExportLayer/exportLayer?layerID=layer_1";
            using var stream = await httpClient.GetStreamAsync(exportUrl);
            using MemoryStream copiedStream = new MemoryStream(); // we need to copy the stream because we want to use it multiple times
            stream.CopyTo(copiedStream);

            // verify contents of export
            using var archive = new ZipArchive(copiedStream, ZipArchiveMode.Read, false);
            var dataFile = archive.GetEntry("data.json");
            Assert.IsNotNull(dataFile);
            var dataStream = dataFile.Open();
            var jd = JsonDocument.Parse(dataStream);
            Assert.IsNotNull(jd);

            // import
            var importUrl = $"{BaseUrl}/api/v1/ImportExportLayer/importLayer?overwriteLayerID=layer_2";
            copiedStream.Position = 0;
            var fileStreamContent = new StreamContent(copiedStream);
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var multipartFormContent = new MultipartFormDataContent();
            multipartFormContent.Add(fileStreamContent, name: "files", fileName: "export.okl1");
            var response = await httpClient.PostAsync(importUrl, multipartFormContent);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            // perform diff
            var ciDiffingQuery = @"query {
              ciDiffing(
                leftLayers: [""layer_1""]
                rightLayers: [""layer_2""]
                showEqual: false
              ) {
                            cis {
                                attributeComparisons {
                                    name
                                }
                            }
                        }
                    }
            ";
            var graphQLResponse3 = await Query(ciDiffingQuery, () => new { ciDiffing = new { cis = new List<object> {} } });
            Assert.IsNull(graphQLResponse3.Errors);
            Assert.AreEqual(0, graphQLResponse3.Data.ciDiffing.cis.Count);
        }
    }
}
