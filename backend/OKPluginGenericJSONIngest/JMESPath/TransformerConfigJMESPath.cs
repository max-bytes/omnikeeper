using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginGenericJSONIngest.JMESPath;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginGenericJSONIngest.JMESPath
{
    public class TransformerConfigJMESPath
    {
        public readonly string Expression;

        public TransformerConfigJMESPath(string expression)
        {
            Expression = expression;
        }
    }
}
