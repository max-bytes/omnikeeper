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
    public class UsecaseMHXDNS
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, JToken>()
            {
                {
                    "listzones.json", JToken.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listzones.json")))
                },
                {
                    "listrecords_mhx-consulting.at.json", JToken.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listrecords_mhx-consulting.at.json")))
                },
                {
                    "listrecords_mhx.at.json", JToken.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                        "data", "usecase_mhx_dns", "listrecords_mhx.at.json")))
                }
            };

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_mhx_dns", "expression.jmes"));

            var transformer = new TransformerJMESPath();
            var result = transformer.Transform(documents, new TransformConfigJMESPath(expression));

            var expected = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>zone>mhx-consulting.at",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "id" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = 718656, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "name", value = "mhx-consulting.at", type = AttributeValueType.Text },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>zone>mhx.at",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "id" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = 742507, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "name", value = "mhx.at", type = AttributeValueType.Text },
                        }
                    },

                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>1569516122",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "id" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = 1569516122, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "name", value = "mhx-consulting.at", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "ttl", value = 86400, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "type", value = "NS", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "value", value = "ns1.he.net", type = AttributeValueType.Text },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>1569516123",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "id" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = 1569516123, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "name", value = "mhx-consulting.at", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "ttl", value = 86400, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "type", value = "NS", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "value", value = "ns2.he.net", type = AttributeValueType.Text },
                        }
                    },
                    new GenericInboundCI
                    {
                        tempID = "tempCIID>record>2569516148",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "id" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "id", value = 2569516148, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "name", value = "mhx.at", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "ttl", value = 86400, type = AttributeValueType.Integer },
                            new GenericInboundAttribute { name = "type", value = "NS", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "value", value = "ns-mhx1.he.net", type = AttributeValueType.Text },
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
            result.Should().BeEquivalentTo(expected);

            string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_mhx_dns", "output.json"), resultJson);
        }
    }
}