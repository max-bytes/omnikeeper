using Omnikeeper.Entity.AttributeValues;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class TextFilterRegexInput
    {
        public readonly string Pattern;
        public readonly RegexOptions Options;

        private readonly Regex compiledRegex;

        public TextFilterRegexInput(string pattern) : this(pattern, System.Array.Empty<RegexOptions>())
        {
        }

        public TextFilterRegexInput(string pattern, RegexOptions[] options)
        {
            Pattern = pattern;
            Options = options.Aggregate(RegexOptions.None, (a, num) => a | num);

            compiledRegex = new Regex(Pattern, Options);
        }

        public bool IsMatch(string v)
        {
            return compiledRegex.IsMatch(v);
        }
    }

    public class AttributeScalarTextFilter
    {
        public TextFilterRegexInput? Regex;
        public string? Exact;

        private AttributeScalarTextFilter() { }

        public static object Build(TextFilterRegexInput? regexObj, string? exact)
        {
            if (regexObj == null && exact == null)
                throw new Exception("At least one filter option needs to be set for AttributeTextFilter");
            return new AttributeScalarTextFilter()
            {
                Exact = exact,
                Regex = regexObj
            };
        }

        // .Net based implementation of this filter
        // must be equivalent to the Postgres based implementation in BaseAttributeModel
        public bool Contains(IAttributeValue attributeValue)
        {
            // type check
            if (attributeValue.Type != AttributeValueType.Text && attributeValue.Type != AttributeValueType.MultilineText)
                return false;
            if (attributeValue.IsArray)
                return false;

            var v = attributeValue.Value2String();

            if (Exact != null)
            {
                if (v != Exact)
                    return false;
            }
            if (Regex != null)
            {
                if (!Regex.IsMatch(v))
                    return false;
            }
            return true;

        }
    }
}
