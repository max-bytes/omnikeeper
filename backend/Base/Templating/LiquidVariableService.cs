using Landscape.Base.Entity;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Collections.Generic;

namespace Landscape.Base.Templating
{
    public static class LiquidVariableService
    {
        // transform dots in variable name into corresponding object structure
        private static void AddNested(Dictionary<string, object> dict, string key, string value)
        {
            // TODO: remove all characters that are not alphanumeric characters or underscores
            // then remove all variables whose key is non-valid (empty parts, ...)

            var splits = key.Split(".");
            var d = dict;
            for (int i = 0; i < splits.Length; i++)
            {
                if (!d.ContainsKey(splits[i]))
                    d.Add(splits[i], new Dictionary<string, object>());

                // what if a key is both a full key and a prefixed-key
                // such as:
                // foo.bar = value1
                // foo.bar.xyz = value2
                // solution:
                // foo.bar.value = value1
                // foo.bar.xyz.value = value2
                // HACK: change string to dictionary and add string to dictionary
                var dod = d[splits[i]];
                if (dod is string)
                {
                    var d0 = dod as string;
                    var nd = new Dictionary<string, object>() { { "", d0 } };
                    d[splits[i]] = nd;
                    dod = nd;
                }

                d = dod as Dictionary<string, object>;
            }
            d.Add("value", value);
        }

        public static Dictionary<string, object> CreateVariablesFromCI(MergedCI ci)
        {
            var targetVariables = new Dictionary<string, object>() { { "ciid", ci.ID }, { "type", ci.Type.ID } };
            foreach (var monitoredCIAttribute in ci.MergedAttributes.Values)
                AddNested(targetVariables, $"{monitoredCIAttribute.Attribute.Name}", monitoredCIAttribute.Attribute.Value.Value2String());
            // TODO: array values
            return targetVariables;
        }
    }
}
