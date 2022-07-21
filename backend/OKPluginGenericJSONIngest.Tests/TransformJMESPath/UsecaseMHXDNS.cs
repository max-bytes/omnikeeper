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
                CIs = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        TempID = "tempCIID>zone>mhx-consulting.at",
                        IDMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "id", Value = new AttributeScalarValueInteger(718656) },
                            new GenericInboundAttribute { Name = "name", Value = new AttributeScalarValueText("mhx-consulting.at") },
                        }
                    },
                    new GenericInboundCI
                    {
                        TempID = "tempCIID>zone>mhx.at",
                        IDMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "id", Value = new AttributeScalarValueInteger(742507) },
                            new GenericInboundAttribute { Name = "name", Value = new AttributeScalarValueText("mhx.at") },
                        }
                    },

                    new GenericInboundCI
                    {
                        TempID = "tempCIID>record>1569516122",
                        IDMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "id", Value = new AttributeScalarValueInteger(1569516122) },
                            new GenericInboundAttribute { Name = "name", Value = new AttributeScalarValueText("mhx-consulting.at") },
                            new GenericInboundAttribute { Name = "ttl", Value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { Name = "type", Value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { Name = "value", Value = new AttributeScalarValueText("ns1.he.net") },
                        }
                    },
                    new GenericInboundCI
                    {
                        TempID = "tempCIID>record>1569516123",
                        IDMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "id", Value = new AttributeScalarValueInteger(1569516123) },
                            new GenericInboundAttribute { Name = "name", Value = new AttributeScalarValueText("mhx-consulting.at") },
                            new GenericInboundAttribute { Name = "ttl", Value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { Name = "type", Value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { Name = "value", Value = new AttributeScalarValueText("ns2.he.net") },
                        }
                    },
                    new GenericInboundCI
                    {
                        TempID = "tempCIID>record>2569516148",
                        IDMethod = new InboundIDMethodByData(new string[]{ "id" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "id", Value = new AttributeScalarValueInteger(2569516148) },
                            new GenericInboundAttribute { Name = "name", Value = new AttributeScalarValueText("mhx.at") },
                            new GenericInboundAttribute { Name = "ttl", Value = new AttributeScalarValueInteger(86400) },
                            new GenericInboundAttribute { Name = "type", Value = new AttributeScalarValueText("NS") },
                            new GenericInboundAttribute { Name = "value", Value = new AttributeScalarValueText("ns-mhx1.he.net") },
                        }
                    },
                },
                Relations = new List<GenericInboundRelation>
                {
                    new GenericInboundRelation
                    {
                        From = "tempCIID>record>1569516122",
                        Predicate = "assigned_to",
                        To = "tempCIID>zone>mhx-consulting.at"
                    },
                    new GenericInboundRelation
                    {
                        From = "tempCIID>record>1569516123",
                        Predicate = "assigned_to",
                        To = "tempCIID>zone>mhx-consulting.at"
                    },
                    new GenericInboundRelation
                    {
                        From = "tempCIID>record>2569516148",
                        Predicate = "assigned_to",
                        To = "tempCIID>zone>mhx.at"
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