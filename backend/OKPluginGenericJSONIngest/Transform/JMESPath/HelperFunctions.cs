using DevLab.JmesPath.Functions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    public class CIIDFunc : JmesPathFunction
    {
        private readonly string fileID;

        public CIIDFunc(string fileID) : base("ciid", 0, true)
        {
            this.fileID = fileID;
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var stringArgs = args.Select(arg => arg.Token.ToString());
            stringArgs = new List<string> { fileID }.Concat(stringArgs);
            return JValue.CreateString(string.Join('>', stringArgs));
        }
    }

    public class AttributeFunc : JmesPathFunction
    {
        public AttributeFunc() : base("attribute", 2, true) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var name = args[0].Token.ToString();
            var values = args[1].Token;
            var type = AttributeValueType.Text.ToString();
            if (args.Length >= 3)
            {
                var typeStr = args[2].Token.ToString();
                try
                {
                    type = Enum.Parse<AttributeValueType>(typeStr).ToString();
                }
                catch (Exception e)
                {
                    throw new Exception($"Cannot parse type \"{typeStr}\" into enum for attribute {name} with values {values}", e);
                }
            }

            object? ConvertItem(JToken item)
            {
                switch (item.Type)
                {
                    case JTokenType.Object:
                        // NOTE: we gracefully handle actual JSON objects as values, and transform them to strings
                        return JsonConvert.SerializeObject(item);
                    case JTokenType.Integer:
                        return ((long)item).ToString();
                    case JTokenType.Float:
                        return ((float)item).ToString();
                    case JTokenType.Boolean:
                        return ((bool)item) ? "true" : "false";
                    case JTokenType.String:
                        return (string)item!;
                    case JTokenType.Null:
                        return null; // TODO: correct?
                    case JTokenType.None:
                    case JTokenType.Date:
                    case JTokenType.Raw:
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                    case JTokenType.TimeSpan:
                    case JTokenType.Property:
                    case JTokenType.Undefined:
                    case JTokenType.Bytes:
                    case JTokenType.Array:
                    case JTokenType.Constructor:
                    case JTokenType.Comment:
                    default:
                        throw new Exception("???");
                }
            }

            // there's a difference between arrays and scalar values when dealing with null values
            // if there's a scalar null value coming in, we set the whole value to null and later processes filter out the whole attribute
            // if there's an array containing null values coming in, we only filter those out and still produce a non-null array, even when it's empty
            if (values is JArray ja)
            {
                var finalValues = new JArray(ja.Select(item => ConvertItem(item)).Where(item => item != null));
                var isArray = true;
                return JObject.FromObject(new { name, value = new { values = finalValues, isArray, type } });
            } else
            {
                var isArray = false;
                var item = ConvertItem(values);
                if (item == null)
                {
                    return JObject.FromObject(new { name, value = JValue.CreateNull() });
                } else
                {
                    var finalValues = new JArray(new object[] { item });
                    return JObject.FromObject(new { name, value = new { values = finalValues, isArray, type } });
                }
            }
        }
    }
    public class RelationFunc : JmesPathFunction
    {
        public RelationFunc() : base("relation", 3) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var from = args[0].Token.ToString();
            var predicate = args[1].Token.ToString();
            var to = args[2].Token.ToString();
            return JObject.FromObject(new { from, predicate, to });
        }
    }



    public class IDMethodByDataFunc : JmesPathFunction
    {
        public IDMethodByDataFunc() : base("idMethodByData", 1) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JArray ja))
                throw new Exception("Invalid attributes when constructing idMethodByData");
            var attributes = ja;
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByData));
            return JObject.FromObject(new { type, attributes });
        }
    }

    public class IDMethodByAttributeFunc : JmesPathFunction
    {
        public IDMethodByAttributeFunc() : base("idMethodByAttribute", 1, true) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JObject ja))
                throw new Exception("Invalid attribute when constructing idMethodByAttribute");

            JObject modifiers = JObject.FromObject(new object());
            if (args.Length >= 2)
            {
                if (!(args[1].Token is JObject jModifiers))
                    throw new Exception("Invalid modifiers when constructing idMethodByAttribute, must be object");

                modifiers = jModifiers;
            }

            var attribute = ja;
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByAttribute));
            return JObject.FromObject(new { type, attribute, modifiers });
        }
    }

    public class IDMethodByRelatedTempIDFunc : JmesPathFunction
    {
        public IDMethodByRelatedTempIDFunc() : base("idMethodByRelatedTempID", 2) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JArray ra))
                throw new Exception("Invalid relation path when constructing idMethodByRelatedData");
            if (ra.Count != 2)
                throw new Exception("Invalid relation path length (must be 2) when constructing idMethodByRelatedData");
            if (!(args[1].Token is JValue jid))
                throw new Exception("Invalid temp ID when constructing idMethodByRelatedData");
            var tempID = jid;
            var outgoingRelationStr = ra[0].ToString();
            var outgoingRelation = outgoingRelationStr == ">";
            var predicateID = ra[1].ToString();
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByRelatedTempID));
            return JObject.FromObject(new { type, tempID, outgoingRelation, predicateID });
        }
    }

    public class IDMethodByTempIDFunc : JmesPathFunction
    {
        public IDMethodByTempIDFunc() : base("idMethodByTempID", 1) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JValue jv))
                throw new Exception("Invalid attributes when constructing idMethodByTempID");
            var tempID = jv;
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByTemporaryCIID));
            return JObject.FromObject(new { type, tempID });
        }
    }

    public class IDMethodByUnionFunc : JmesPathFunction
    {
        public IDMethodByUnionFunc() : base("idMethodByUnion", 1) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JArray i))
                throw new Exception("Invalid inner idMethods when constructing idMethodByUnion");
            var inner = i;
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByByUnion));
            return JObject.FromObject(new { type, inner });
        }
    }
    public class IDMethodByIntersectFunc : JmesPathFunction
    {
        public IDMethodByIntersectFunc() : base("idMethodByIntersect", 1) { }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            if (!(args[0].Token is JArray i))
                throw new Exception("Invalid inner idMethods when constructing idMethodByIntersect");
            var inner = i;
            var type = SystemTextJSONSerializerMigrationHelper.GetTypeString(typeof(InboundIDMethodByIntersect));
            return JObject.FromObject(new { type, inner });
        }
    }

    public class RegexIsMatchFunc : JmesPathFunction
    {
        public RegexIsMatchFunc() : base("regexIsMatch", 2)
        {
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var regexStr = args[0].Token.ToString();
            var subject = args[1].Token.ToString();
            var regex = new Regex(regexStr);
            var matches = regex.IsMatch(subject);
            return new JValue(matches);
        }
    }

    public class RegexMatchFunc : JmesPathFunction
    {
        public RegexMatchFunc() : base("regexMatch", 2)
        {
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var regexStr = args[0].Token.ToString();
            var subject = args[1].Token.ToString();
            var regex = new Regex(regexStr);
            var matches = regex.Match(subject);
            var matchesString = matches.Groups.Values.Select(m => m.Value);
            return JArray.FromObject(matchesString);
        }
    }

    public class StoreFunc : JmesPathFunction
    {
        private readonly IDictionary<string, string> values;

        public StoreFunc(IDictionary<string, string> values) : base("store", 2)
        {
            this.values = values;
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var name = args[0].Token.ToString();
            var value = args[1].Token.ToString();
            values.AddOrUpdate(name, value);
            return new JValue(value);
        }
    }

    public class RetrieveFunc : JmesPathFunction
    {
        private readonly IDictionary<string, string> values;

        public RetrieveFunc(IDictionary<string, string> values) : base("retrieve", 1)
        {
            this.values = values;
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var name = args[0].Token.ToString();
            var value = values[name];
            return new JValue(value);
        }
    }

    public class IndexBuilder : JmesPathFunction
    {
        private readonly IDictionary<string, int> indices;

        public IndexBuilder() : base("idx", 1)
        {
            indices = new Dictionary<string, int>();
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var indexName = args[0].Token.ToString();
            var index = indices.GetOr(indexName, 0);
            indices.AddOrUpdate(indexName, () => 1, (idx) => idx + 1);
            return JValue.CreateString(index.ToString());
        }
    }

    public class FilterHashKeys : JmesPathFunction
    {
        public FilterHashKeys() : base("filterHashKeys", 2)
        {
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var validKeys = args[0].Token;
            var subject = args[1].Token;
            return new JObject(subject.Children<JProperty>().Where(jp => validKeys.Values().Contains(jp.Name)));
        }
    }

    public class StringReplaceFunc : JmesPathFunction
    {
        public StringReplaceFunc() : base("stringReplace", 3)
        {
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var subject = args[0].Token;
            var replace = args[1].Token;
            var replaceWith = args[2].Token;
            return JValue.CreateString(subject.ToString().Replace(replace.ToString(), replaceWith.ToString()));
        }
    }
}
