using FluentAssertions;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.IO;

namespace OKPluginGenericJSONIngest.Tests.TransformJMESPath
{
    public class UsecaseECMInvScan
    {
        private JToken ParseJSONString(string s)
        {
            using var jsonReader = new JsonTextReader(new StringReader(s))
            {
                DateParseHandling = DateParseHandling.None // TODO: ensure that we always set this!
            };
            return JToken.ReadFrom(jsonReader);
        }

        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, JToken>() {
                {
                    "inventory_scan_windows", ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ecm_inv_scan", "input.json")))
                },
            };

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "expression.jmes"));


            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(expression));
            var inputJSON = transformer.Documents2JSON(documents);
            var genericInboundDataJson = transformer.TransformJSON(inputJSON);

            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "output_intermediate.json"), genericInboundDataJson.ToString(Formatting.Indented));

            var result = transformer.DeserializeJson(genericInboundDataJson);

            //var tmp = JsonConvert.SerializeObject(result);

            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None // TODO: move?
            };
            var expected = JsonConvert.DeserializeObject<GenericInboundData>(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ecm_inv_scan", "expected.json")), settings);

            result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

            string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "output.json"), resultJson);
        }
    }
}