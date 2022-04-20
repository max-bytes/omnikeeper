using FluentAssertions;
using Microsoft.DotNet.InternalAbstractions;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Tests.TransformJMESPath
{
    public class UsecaseMHXDNS
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, string>()
            {
                {
                    "listzones.json", (File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listzones.json")))
                },
                {
                    "listrecords_mhx-consulting.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listrecords_mhx-consulting.at.json")))
                },
                {
                    "listrecords_mhx.at.json", (File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listrecords_mhx.at.json")))
                }
            };

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_mhx_dns", "expression.jmes"));

            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(expression));
            var result = transformer.Transform(documents);

            var expected = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>zone>mhx-consulting.at",
                        idMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = new AttributeScalarValueInteger(718656) },
                            new GenericInboundAttribute { name = "name", value = new AttributeScalarValueText("mhx-consulting.at") },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>zone>mhx.at",
                        idMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = new AttributeScalarValueInteger(742507) },
                            new GenericInboundAttribute { name = "name", value = new AttributeScalarValueText("mhx.at") },
                        }
                    },

                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>1569516122",
                        idMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = new AttributeScalarValueInteger(1569516122) },
                            new GenericInboundAttribute { name = "name", value = new AttributeScalarValueText("mhx-consulting.at") },
                            new GenericInboundAttribute { name = "ttl", value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { name = "type", value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { name = "value", value = new AttributeScalarValueText("ns1.he.net") },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>1569516123",
                        idMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = new AttributeScalarValueInteger(1569516123) },
                            new GenericInboundAttribute { name = "name", value = new AttributeScalarValueText("mhx-consulting.at") },
                            new GenericInboundAttribute { name = "ttl", value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { name = "type", value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { name = "value", value = new AttributeScalarValueText("ns2.he.net") },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>2569516148",
                        idMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = new AttributeScalarValueInteger(2569516148) },
                            new GenericInboundAttribute { name = "name", value = new AttributeScalarValueText("mhx.at") },
                            new GenericInboundAttribute { name = "ttl", value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { name = "type", value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { name = "value", value = new AttributeScalarValueText("ns-mhx1.he.net") },
                        }
                    },
                },
                relations = new List<GenericInboundRelation>
                {
                    new GenericInboundRelation
                    {
                        from = "tempCIID>record>1569516122",
                        predicate = "assigned_to",
                        to = "tempCIID>zone>mhx-consulting.at"
                    },
                    new GenericInboundRelation
                    {
                        from = "tempCIID>record>1569516123",
                        predicate = "assigned_to",
                        to = "tempCIID>zone>mhx-consulting.at"
                    },
                    new GenericInboundRelation
                    {
                        from = "tempCIID>record>2569516148",
                        predicate = "assigned_to",
                        to = "tempCIID>zone>mhx.at"
                    },
                }
            };
            result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

            string resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions()
            {
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter()
                },
                WriteIndented = true
            });
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_mhx_dns", "output.json"), resultJson);
        }
    }
}