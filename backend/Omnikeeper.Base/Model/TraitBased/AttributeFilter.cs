using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class AttributeFilter
    {
        public readonly string attributeName;
        // TODO: support non-text filters
        public readonly AttributeScalarTextFilter filter;

        public AttributeFilter(string attributeName, AttributeScalarTextFilter filter)
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

    public class AttributeScalarTextFilter
    {
        public TextFilterRegexInput? Regex;
        public string? Exact;
        public bool? IsSet;

        private AttributeScalarTextFilter() { }

        public static AttributeScalarTextFilter Build(TextFilterRegexInput? regexObj, string? exact, bool? isSet)
        {
            if (regexObj == null && exact == null && isSet == null)
                throw new Exception("At least one filter option needs to be set for AttributeTextFilter");
            return new AttributeScalarTextFilter()
            {
                Exact = exact,
                Regex = regexObj,
                IsSet = isSet
            };
        }
    }
}
