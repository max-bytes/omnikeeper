using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    public class TransformerJMESPath
    {
        private readonly JmesPath jmes;
        private readonly JmesPath.Expression expression;

        public static TransformerJMESPath Build(TransformConfigJMESPath config)
        {
            var tmpValues = new Dictionary<string, string>();

            var jmes = new JmesPath();
            jmes.FunctionRepository.Register("ciid", new CIIDFunc("tempCIID"));
            jmes.FunctionRepository.Register("attribute", new AttributeFunc());
            jmes.FunctionRepository.Register("relation", new RelationFunc());
            jmes.FunctionRepository.Register("idMethodByData", new IDMethodByDataFunc());
            jmes.FunctionRepository.Register("idMethodByTempID", new IDMethodByTempIDFunc());
            jmes.FunctionRepository.Register("idx", new IndexBuilder());
            jmes.FunctionRepository.Register("regexIsMatch", new RegexIsMatchFunc());
            jmes.FunctionRepository.Register("regexMatch", new RegexMatchFunc());
            jmes.FunctionRepository.Register("store", new StoreFunc(tmpValues));
            jmes.FunctionRepository.Register("retrieve", new RetrieveFunc(tmpValues));
            jmes.FunctionRepository.Register("filterHashKeys", new FilterHashKeys());
            jmes.FunctionRepository.Register("stringReplace", new StringReplaceFunc());

            var expression = jmes.Parse(config.Expression);

            return new TransformerJMESPath(jmes, expression);
        }

        private TransformerJMESPath(JmesPath jmes, JmesPath.Expression expression)
        {
            this.jmes = jmes;
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

        public JArray Documents2JSON(IDictionary<string, Func<Stream>> documents)
        {
            var input = new JArray();
            foreach (var kv in documents)
            {
                using var stream = kv.Value();
                using var reader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.None // TODO: ensure that we always set this!
                };
                var data = JToken.ReadFrom(jsonReader);
                input.Add(new JObject
                {
                    ["document"] = kv.Key,
                    ["data"] = data
                });
            }
            return input;
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

        //public Exception? ValidateConfig(TransformConfigJMESPath config)
        //{
        //    try
        //    {
        //        var parsed = jmes.Parse(config.Expression);
        //    } catch (Exception e)
        //    {
        //        return e;
        //    }
        //    return null;
        //}

        //public string TransformJSON(string input, TransformConfigJMESPath config)
        //{
        //    var resultJson = jmes.Transform(input, config.Expression);
        //    return resultJson;
        //}

        public JToken TransformJSON(JArray input)
        {
            // NOTE: jmes.Transform with a JToken as input is marked as obsolete, but works for our case and is much more performant
            // see https://github.com/jdevillard/JmesPath.Net/blob/master/src/jmespath.net/JmesPath.cs#L33
#pragma warning disable CS0618
            var resultJson = expression.Transform(input);
#pragma warning restore CS0618
            return resultJson.AsJToken();
        }
    }
}
