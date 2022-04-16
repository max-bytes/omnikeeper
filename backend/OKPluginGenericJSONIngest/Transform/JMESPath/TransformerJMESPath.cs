using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    public class TransformerJMESPath
    {
        private readonly JmesPath.Expression expression;

        public static TransformerJMESPath Build(TransformConfigJMESPath config)
        {
            var tmpValues = new Dictionary<string, string>();

            var jmes = new JmesPath();
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("tempCIID"));
            jmes.FunctionRepository.Register("attribute", new AttributeFunc());
            jmes.FunctionRepository.Register("relation", new RelationFunc());
            jmes.FunctionRepository.Register("idMethodByData", new IDMethodByDataFunc());
            jmes.FunctionRepository.Register("idMethodByAttribute", new IDMethodByAttributeFunc());
            jmes.FunctionRepository.Register("idMethodByRelatedTempID", new IDMethodByRelatedTempIDFunc());
            jmes.FunctionRepository.Register("idMethodByTempID", new IDMethodByTempIDFunc());
            jmes.FunctionRepository.Register("idMethodByUnion", new IDMethodByUnionFunc());
            jmes.FunctionRepository.Register("idMethodByIntersect", new IDMethodByIntersectFunc());
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            jmes.FunctionRepository.Register("regexIsMatch", new RegexIsMatchFunc());
            jmes.FunctionRepository.Register("regexMatch", new RegexMatchFunc());
            jmes.FunctionRepository.Register("store", new StoreFunc(tmpValues));
            jmes.FunctionRepository.Register("retrieve", new RetrieveFunc(tmpValues));
            jmes.FunctionRepository.Register("filterHashKeys", new FilterHashKeys());
            jmes.FunctionRepository.Register("stringReplace", new StringReplaceFunc());

            var expression = jmes.Parse(config.Expression);

            return new TransformerJMESPath(expression);
        }

        private TransformerJMESPath(JmesPath.Expression expression)
        {
            this.expression = expression;
        }

        public GenericInboundData Transform(IDictionary<string, JToken> documents)
        {
            var input = Documents2JSON(documents);
            var resultJson = TransformJSON(input);
            return DeserializeJson(resultJson);
        }

        public GenericInboundData DeserializeJson(JToken resultJson)
        {
            var settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None // TODO: move?
            };
            var serializer = JsonSerializer.Create(settings);
            var r = resultJson.ToObject<GenericInboundData>(serializer);
            if (r == null)
                throw new Exception("Could not deserialize JSON into GenericInboundData");

            return r;
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

        public JToken TransformJSON(JToken input)
        {
            // NOTE: jmes.Transform with a JToken as input is marked as obsolete, but works for our case and is much more performant
            // see https://github.com/jdevillard/JmesPath.Net/blob/master/src/jmespath.net/JmesPath.cs#L33
            var resultJson = expression.Transform(input);
            return resultJson.AsJToken();
        }
    }
}
