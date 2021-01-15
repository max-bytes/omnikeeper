using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OKPluginGenericJSONIngest.JMESPath;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginGenericJSONIngest
{
    public class Transformer
    {
        public GenericInboundData Transform(IDictionary<string, JToken> documents, string expression)
        {
            var input = new JArray();
            foreach(var kv in documents)
            {
                input.Add(new JObject
                {
                    ["document"] = kv.Key,
                    ["data"] = kv.Value
                });
            }

            var tmpValues = new Dictionary<string, string>();

            var jmes = new JmesPath();
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("tempCIID")); // TODO: prefix for CIIDFunc
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            jmes.FunctionRepository.Register("regexIsMatch", new RegexIsMatchFunc());
            jmes.FunctionRepository.Register("regexMatch", new RegexMatchFunc());
            jmes.FunctionRepository.Register("store", new StoreFunc(tmpValues));
            jmes.FunctionRepository.Register("retrieve", new RetrieveFunc(tmpValues));
            var resultJson = jmes.Transform(input.ToString(), expression);

            var r = JsonConvert.DeserializeObject<GenericInboundData>(resultJson);

            return r;
        }
    }
}
