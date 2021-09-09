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
    public class UsecaseAnsibleInventory
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
            var documents = new Dictionary<string, JToken>()
            {
                {
                    "setup_facts_h1jmplx01.mhx.at.json", ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "setup_facts_h1jmplx01.mhx.at.json"))) 
                },
                {
                    "yum_installed_h1jmplx01.mhx.at.json", ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_installed_h1jmplx01.mhx.at.json")))
                },
                {
                    "yum_repos_h1jmplx01.mhx.at.json", ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_repos_h1jmplx01.mhx.at.json")))
                },
                {
                    "yum_updates_h1jmplx01.mhx.at.json", ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_updates_h1jmplx01.mhx.at.json")))
                },
                {
                    "setup_facts_h1lscapet01.mhx.local.json",ParseJSONString(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "setup_facts_h1lscapet01.mhx.local.json")))
                }
            };

            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(AnsibleInventoryScanJMESPathExpression.Expression));
            var inputJSON = transformer.Documents2JSON(documents);
            var genericInboundDataJson = transformer.TransformJSON(inputJSON);

            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "output_intermediate.json"), genericInboundDataJson.ToString(Formatting.Indented));

            var result = transformer.DeserializeJson(genericInboundDataJson);

            //var tmp = JsonConvert.SerializeObject(result);

            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None // TODO: move?
            };
            var expected = JsonConvert.DeserializeObject<GenericInboundData>(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "expected.json")), settings);

            result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

            string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "output.json"), resultJson);
        }
    }
}