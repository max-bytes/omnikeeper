using DevLab.JmesPath.Functions;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
            var value = args[1].Token;
            if (value is JValue jv && jv.Value == null)
            {
                return null;
            }
            var type = AttributeValueType.Text.ToString();
            if (args.Length >= 3)
                type = Enum.Parse<AttributeValueType>(args[2].Token.ToString()).ToString();
            return JObject.FromObject(new { name, value, type });
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
            var method = "byData";
            return JObject.FromObject(new { method, attributes });
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
            var method = "byTempID";
            return JObject.FromObject(new { method, tempID });
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
