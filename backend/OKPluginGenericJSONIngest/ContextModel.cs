using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OKPluginGenericJSONIngest
{
    public interface IContextModel
    {
        Context? GetContextByName(string name);
    }

    public class ContextModel : IContextModel
    {
        private readonly IDictionary<string, Context> contexts = new List<Context>()
        {
            new Context("ansibleInventoryScan", 
                new ExtractConfigPassiveRESTFiles(), 
                new TransformConfigJMESPath(AnsibleInventoryScanJMESPathExpression.Expression), 
                new LoadConfig(new long[] {1,2 }, 2))
        }.ToDictionary(c => c.Name); // TODO: make configurable

        public Context? GetContextByName(string name)
        {
            contexts.TryGetValue(name, out var c);
            return c;
        }
    }
}
