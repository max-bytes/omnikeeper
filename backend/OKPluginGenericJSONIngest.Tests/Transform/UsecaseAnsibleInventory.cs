using DevLab.JmesPath;
using FluentAssertions;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.JMESPath;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.IO;

namespace OKPluginGenericJSONIngest.Tests.Transform
{
    public class UsecaseAnsibleInventory
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, JToken>()
            {
                {
                    "setup_facts.json", JToken.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_ansible_inventory", "setup_facts.json")))
                }
            };

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "expression.jmes"));

            var transformer = new TransformerJMESPath();
            var result = transformer.Transform(documents, expression);

            var expected = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "tempCIID",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{"fqdn"} },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "fqdn", value = "h1jmplx01.mhx.at", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "os_family", value = "x86_64", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "ansible.inventory.cmdline", value = JObject.Parse(@"{
                                ""BOOT_IMAGE"": ""/vmlinuz-3.10.0-1062.18.1.el7.x86_64"",
                                ""LANG"": ""en_US.UTF-8"",
                                ""quiet"": true,
                                ""rd.lvm.lv"": ""vg_sys/lv_swap"",
                                ""rhgb"": true,
                                ""ro"": true,
                                ""root"": ""/dev/mapper/vg_sys-lv_root""
                              }"), type = AttributeValueType.JSON },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>ansible_mounts>0",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{"__name", "device", "mount"} },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "__name", value = "h1jmplx01.mhx.at:/boot", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "device", value = "/dev/sda1", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "mount", value = "/boot", type = AttributeValueType.Text },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>ansible_mounts>1",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{"__name", "device", "mount"} },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "__name", value = "h1jmplx01.mhx.at:/data", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "device", value = "/dev/mapper/vg_data-lv_data", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "mount", value = "/data", type = AttributeValueType.Text },
                        }
                    },
                },
                relations = new List<GenericInboundRelation>
                {
                    new GenericInboundRelation
                    {
                        from = "tempCIID",
                        predicate = "has_mounted_device",
                        to = "tempCIID>ansible_mounts>0"
                    },
                    new GenericInboundRelation
                    {
                        from = "tempCIID",
                        predicate = "has_mounted_device",
                        to = "tempCIID>ansible_mounts>1"
                    },
                }
            };
            result.Should().BeEquivalentTo(expected);

            string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_ansible_inventory", "output.json"), resultJson);
        }
    }
}