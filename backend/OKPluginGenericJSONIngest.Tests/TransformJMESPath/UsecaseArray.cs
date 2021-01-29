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
    public class UsecaseArray
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, JToken>() { }; // no input documents, expression generates output on its own

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_array", "expression.jmes"));

            var transformer = new TransformerJMESPath();
            var result = transformer.Transform(documents, new TransformConfigJMESPath(expression));

            // test that attribute values that are arrays are also properly deserialized 
            var expected = new GenericInboundData
            {
                cis = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        tempID = "tempCIID",
                        idMethod = new GenericInboundIDMethod { method = "byData", attributes = new string[]{ "foo" } },
                        attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { name = "textscalar", value = "value 1", type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "textarray", value = new string[] {"value 1", "value 2" }, type = AttributeValueType.Text },
                            new GenericInboundAttribute { name = "jsonarray", value = new JToken[] {
                                JToken.Parse(@"{foo: 'bar', blub: 'bla'}"),
                                JToken.Parse(@"{ foo2: 'bar2', blub2: 'bla2'}"), 
                            }, type = AttributeValueType.JSON },
                        }
                    },
                },
                relations = new List<GenericInboundRelation> { }
            };
            result.Should().BeEquivalentTo(expected);

            string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "usecase_array", "output.json"), resultJson);
        }
    }
}