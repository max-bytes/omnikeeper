using DevLab.JmesPath;
using Microsoft.DotNet.InternalAbstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OKPluginGenericJSONIngest.JMESPath;
using System.IO;

namespace OKPluginGenericJSONIngest.Tests
{
    public class UsecaseAnsibleInventory
    {
        [Test]
        public void Test1()
        {
            string input = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(), 
                "contrib", "usecase_ansible_inventory", "setup_facts.json"));

            string expression = File.ReadAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_ansible_inventory", "expression.jmes"));

            var jmes = new JmesPath();
            //jmes.FunctionRepository.Register("fileID", new FileID("setup_facts.json"));
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("listzones.json"));
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            var result = jmes.Transform(input, expression);

            var token = JToken.Parse(result);
            var text = token.ToString();

            File.WriteAllText(Path.Combine(Directory.GetParent(ApplicationEnvironment.ApplicationBasePath).Parent.Parent.Parent.ToString(),
                "contrib", "usecase_ansible_inventory", "output.json"), text);
        }
    }
}