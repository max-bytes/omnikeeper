﻿using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest.JMESPath
{
    public class TransformerJMESPath
    {
        public GenericInboundData Transform(IDictionary<string, JToken> documents, TransformerConfigJMESPath config)
        {
            var input = Documents2JSON(documents);
            var resultJson = TransformJSON(input, config);
            return DeserializeJson(resultJson);
        }

        public GenericInboundData DeserializeJson(string resultJson)
        {
            return JsonConvert.DeserializeObject<GenericInboundData>(resultJson);
        }

        public JArray Documents2JSON(IDictionary<string, JToken> documents)
        {
            var input = new JArray();
            foreach (var kv in documents)
            {
                input.Add(new JObject
                {
                    ["document"] = kv.Key,
                    ["data"] = kv.Value
                });
            }
            return input;
        }

        public string TransformJSON(JArray input, TransformerConfigJMESPath config)
        {
            var tmpValues = new Dictionary<string, string>();

            var jmes = new JmesPath();
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("tempCIID"));
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            jmes.FunctionRepository.Register("regexIsMatch", new RegexIsMatchFunc());
            jmes.FunctionRepository.Register("regexMatch", new RegexMatchFunc());
            jmes.FunctionRepository.Register("store", new StoreFunc(tmpValues));
            jmes.FunctionRepository.Register("retrieve", new RetrieveFunc(tmpValues));
            jmes.FunctionRepository.Register("filterHashKeys", new FilterHashKeys());
            jmes.FunctionRepository.Register("stringReplace", new StringReplaceFunc());
            var resultJson = jmes.Transform(input.ToString(), config.Expression);
            return resultJson;
        }
    }
}
