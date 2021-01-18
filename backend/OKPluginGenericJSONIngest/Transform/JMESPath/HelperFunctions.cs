using DevLab.JmesPath.Functions;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Utils;
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
            var stringArgs = args.Select(arg => arg.Token.ToString()); // TODO: what if arg is an expression instead of a token?
            stringArgs = new List<string> { fileID }.Concat(stringArgs);
            return JValue.CreateString(string.Join('>', stringArgs)); // TODO: do we need to escape @ in strings?
        }
    }

    public class RegexIsMatchFunc : JmesPathFunction
    {
        public RegexIsMatchFunc() : base("regexIsMatch", 2)
        {
        }

        public override JToken Execute(params JmesPathFunctionArgument[] args)
        {
            var regexStr = args[0].Token.ToString(); // TODO: what if arg is an expression instead of a token?
            var subject = args[1].Token.ToString(); // TODO: what if arg is an expression instead of a token?
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
            var regexStr = args[0].Token.ToString(); // TODO: what if arg is an expression instead of a token?
            var subject = args[1].Token.ToString(); // TODO: what if arg is an expression instead of a token?
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
            var name = args[0].Token.ToString(); // TODO: what if arg is an expression instead of a token?
            var value = args[1].Token.ToString(); // TODO: what if arg is an expression instead of a token?
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
            var name = args[0].Token.ToString(); // TODO: what if arg is an expression instead of a token?
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
