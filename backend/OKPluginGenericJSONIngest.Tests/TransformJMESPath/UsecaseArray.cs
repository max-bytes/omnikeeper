using FluentAssertions;
using NUnit.Framework;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OKPluginGenericJSONIngest.Tests.TransformJMESPath
{
    public class UsecaseArray
    {
        [Test]
        public void Test1()
        {
            var documents = new Dictionary<string, string>() { }; // no input documents, expression generates output on its own

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                "data", "usecase_array", "expression.jmes"));


            var transformer = TransformerJMESPath.Build(new TransformConfigJMESPath(expression));
            var result = transformer.Transform(documents);

            // test that attribute values that are arrays are also properly deserialized 
            var expected = new GenericInboundData
            {
                CIs = new List<GenericInboundCI>
                {
                    new GenericInboundCI
                    {
                        TempID = "tempCIID",
                        IDMethod = new InboundIDMethodByData(new string[]{ "foo" }),
                        Attributes = new List<GenericInboundAttribute>
                        {
                            new GenericInboundAttribute { Name = "textscalar", Value = AttributeValueDTO.Build(new AttributeScalarValueText("value 1")) },
                            new GenericInboundAttribute { Name = "textarray", Value = AttributeValueDTO.Build(AttributeArrayValueText.BuildFromString(new string[] {"value 1", "value 2" })) },
                            new GenericInboundAttribute { Name = "jsonarray", Value = AttributeValueDTO.Build(AttributeArrayValueJSON.BuildFromString(new string [] {
                                @"{""foo"":""bar"",""blub"":""bla""}",
                                @"{""foo2"":""bar2"",""blub2"":""bla2""}"
                            }, false))},
                        }
                    },
                },
                Relations = new List<GenericInboundRelation> { }
            };
            result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());

            string resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(Path.Combine(Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.ToString(),
                "data", "usecase_array", "output.json"), resultJson);
        }
    }
}