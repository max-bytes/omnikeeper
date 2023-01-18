using FluentAssertions;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Tests.TransformJMESPath
{
    public class UsecaseAnsibleInventory
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, string>()
            {
                {
                    "setup_facts_h1jmplx01.mhx.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "setup_facts_h1jmplx01.mhx.at.json")))
                },
                {
                    "yum_installed_h1jmplx01.mhx.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_installed_h1jmplx01.mhx.at.json")))
                },
                {
                    "yum_repos_h1jmplx01.mhx.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_repos_h1jmplx01.mhx.at.json")))
                },
                {
                    "yum_updates_h1jmplx01.mhx.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "yum_updates_h1jmplx01.mhx.at.json")))
                },
                {
                    "setup_facts_h1lscapet01.mhx.local.json",(File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "setup_facts_h1lscapet01.mhx.local.json")))
                }
            };

            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(AnsibleInventoryScanJMESPathExpression.Expression));
            var inputJSON = transformer.Documents2JSON(documents);
            var genericInboundDataJson = transformer.TransformJSON(inputJSON);

            File.WriteAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "output_intermediate.json"), genericInboundDataJson);

            var result = transformer.DeserializeJson(genericInboundDataJson);

            string resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions()
            {
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter()
                },
                WriteIndented = true
            });
            File.WriteAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "output.json"), resultJson);

            var expected = JsonSerializer.Deserialize<GenericInboundData>(File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "expected.json")), new JsonSerializerOptions()
                        {
                            IncludeFields = true,
                            Converters = {
                                new JsonStringEnumConverter()
                            }
                        });

            result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            
        }
    }
}