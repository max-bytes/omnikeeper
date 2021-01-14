using DevLab.JmesPath;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.JMESPath;
using System.Collections.Generic;
using System.IO;

namespace OKPluginGenericJSONIngest.Tests
{
    public class UsecaseMHXDNS
    {
        [Test]
        public void Test1()
        {
            string inputZones = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(), 
                "contrib", "usecase_mhx_dns", "listzones.json"));
            string inputRecords1 = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_mhx_dns", "listrecords_mhx-consulting.at.json"));
            string inputRecords2 = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_mhx_dns", "listrecords_mhx.at.json"));

            var fullInput = new JArray();
            fullInput.Add(new JObject
            {
                ["filename"] = "listzones.json",
                ["data"] = JToken.Parse(inputZones)
            });
            fullInput.Add(new JObject
            {
                ["filename"] = "listrecords_mhx-consulting.at.json",
                ["data"] = JToken.Parse(inputRecords1)
            });
            fullInput.Add(new JObject
            {
                ["filename"] = "listrecords_mhx.at.json",
                ["data"] = JToken.Parse(inputRecords2)
            });

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_mhx_dns", "expression.jmes"));

            var tmpValues = new Dictionary<string, string>();

            var jmes = new JmesPath();
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("")); // TODO: prefix for CIIDFunc
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            jmes.FunctionRepository.Register("regex", new RegexMatchFunc());
            jmes.FunctionRepository.Register("store", new StoreFunc(tmpValues));
            jmes.FunctionRepository.Register("retrieve", new RetrieveFunc(tmpValues));
            var result = jmes.Transform(fullInput.ToString(), expression);

            var token = JToken.Parse(result);
            var text = token.ToString();

            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_mhx_dns", "output.json"), text);
        }
    }
}