using FluentAssertions;
using Microsoft.DotNet.InternalAbstractions;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Tests.TransformJMESPath
{
    public class UsecaseECMInvScan
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, string>() {
                {
                    "inventory_scan_windows", (File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ecm_inv_scan", "input_win.json")))
                },
                {
                    "inventory_scan_linux", (File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ecm_inv_scan", "input_linux.json")))
                },
            };

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "expression.jmes"));


            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(expression));
            var inputJSON = transformer.Documents2JSON(documents);
            var genericInboundDataJson = transformer.TransformJSON(inputJSON);

            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "output_intermediate.json"), genericInboundDataJson.ToString());

            var result = transformer.DeserializeJson(genericInboundDataJson);

            string resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions() {
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter()
                },
                WriteIndented = true
            });
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ecm_inv_scan", "output.json"), resultJson);

            var expected = JsonSerializer.Deserialize<GenericInboundData>(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ecm_inv_scan", "expected.json")), new JsonSerializerOptions()
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