using Microsoft.DotNet.PlatformAbstractions;
using NUnit.Framework;
using OKPluginGenericJSONIngest;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using System;
using System.IO;

namespace OKPluginInsightDiscoveryScanIngest.Tests
{
    public class Tests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            
            var transformerConfig = new TransformConfigJMESPath(DefaultJMESPathExpression.Expression);
            var transformer = TransformerJMESPath.Build(transformerConfig);

            string inputJson = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "data", "small_input.json"));

            string transformedOutput;
            try
            {
                transformedOutput = transformer.TransformJSON(inputJson);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
                return;
            }

            GenericInboundData genericInboundData;
            try
            {
                genericInboundData = transformer.DeserializeJson(transformedOutput);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
                return;
            }


        }
    }
}