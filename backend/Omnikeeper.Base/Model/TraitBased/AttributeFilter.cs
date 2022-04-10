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
            Options = options.Aggregate(RegexOptions.None, (a,num) => a | num);

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
    }

    public static class AttributeFilterHelper
    {
        public static bool Matches(IAttributeValue attributeValue, AttributeScalarTextFilter filter)
        {
            // type check
            if (attributeValue.Type != AttributeValueType.Text && attributeValue.Type != AttributeValueType.MultilineText)
                return false;
            if (attributeValue.IsArray)
                return false;

            var v = attributeValue.Value2String();

            if (filter.Exact != null)
            {
                if (v != filter.Exact)
                    return false;
            }
            if (filter.Regex != null)
            {
                if (!filter.Regex.IsMatch(v))
                    return false;
            }
            return true;
        }
    }

}
