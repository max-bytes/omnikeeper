using GraphQL.Types;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class AttributeScalarTextFilter
    {
        public Regex? Regex;
        public string? Exact;
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
