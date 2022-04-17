using DevLab.JmesPath;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public GenericInboundData Transform(IDictionary<string, string> documents)
        {
            var input = Documents2JSON(documents);
            var resultJson = TransformJSON(input);
            return DeserializeJson(resultJson);
        }

        public GenericInboundData DeserializeJson(string resultJson)
        {
            //var settings = new JsonSerializerSettings
            //{
            //    DateParseHandling = DateParseHandling.None // TODO: move?
            //};
            ////var r = JsonSerializer.Create(settings);
            //var r = JsonConvert.DeserializeObject<GenericInboundData>(resultJson, settings);// resultJson.ToObject<GenericInboundData>(serializer);
            var r = JsonSerializer.Deserialize<GenericInboundData>(resultJson, new JsonSerializerOptions()
            {
                IncludeFields = true,
                Converters = {
                    new JsonStringEnumConverter()
                },
            });
            if (r == null)
                throw new Exception("Could not deserialize JSON into GenericInboundData");

            return r;
        }

        public string Documents2JSON(IDictionary<string, string> documents)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            var first = true;
            foreach (var kv in documents)
            {
                if (first)
                {
                    first = false;
                } else
                {
                    sb.Append(",");
                }
                sb.Append("{");
                sb.Append($"\"document\": \"{kv.Key}\",");
                sb.Append($"\"data\": {kv.Value}");
                sb.Append("}");
            }
            sb.Append("]");
            return sb.ToString();
            //var input = new JArray();
            //foreach (var kv in documents)
            //{
            //    input.Add(new JObject
            //    {
            //        ["document"] = kv.Key,
            //        ["data"] = kv.Value
            //    });
            //}
            //return input;
        }

        public string TransformJSON(string input)
        {
            var resultJson = expression.Transform(input);
            return resultJson;
        }
    }
}
