using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class AttributeFilter
    {
        public readonly string attributeName;
        public readonly IAttributeFilter filter;

        public AttributeFilter(string attributeName, IAttributeFilter filter)
        {
            this.attributeName = attributeName;
            this.filter = filter;
        }
    }

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

    public interface IAttributeFilter
    {

    }

    public class AttributeScalarTextFilter : IAttributeFilter
    {
        public TextFilterRegexInput? Regex;
        public string? Exact;
        public bool? IsSet;

        private AttributeScalarTextFilter() { }

        public static AttributeScalarTextFilter Build(TextFilterRegexInput? regexObj, string? exact, bool? isSet)
        {
            if (regexObj == null && exact == null && isSet == null)
                throw new Exception("At least one filter option needs to be set for AttributeScalarTextFilter");
            return new AttributeScalarTextFilter()
            {
                Exact = exact,
                Regex = regexObj,
                IsSet = isSet
            };
        }
    }

    public class AttributeScalarBooleanFilter : IAttributeFilter
    {
        public bool? IsTrue;
        public bool? IsSet;

        private AttributeScalarBooleanFilter() { }

        public static AttributeScalarBooleanFilter Build(bool? isTrue, bool? isSet)
        {
            if (isTrue == null && isSet == null)
                throw new Exception("At least one filter option needs to be set for AttributeScalarBooleanFilter");
            return new AttributeScalarBooleanFilter()
            {
                IsTrue = isTrue,
                IsSet = isSet
            };
        }
    }
}
