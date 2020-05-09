using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public abstract class AttributeValueJSON : IAttributeValue
    {
        public override string ToString() => $"AV-JSON: {Value2String()}";
        public AttributeValueType Type => AttributeValueType.JSON;
        public abstract string Value2String();
        public abstract bool IsArray { get; }
        public abstract AttributeValueDTO ToDTO();
        public abstract object ToGenericObject();
        public abstract bool Equals(IAttributeValue other);
        public abstract bool FullTextSearch(string searchString, CompareOptions compareOptions);

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for JSON
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        { // does not make sense for JSON
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

    }

    public class AttributeValueJSONScalar : AttributeValueJSON, IEquatable<AttributeValueJSONScalar>
    {
        public JToken Value { get; private set; }
        public override string Value2String() => Value.ToString();
        public override AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Value.ToString(), Type);
        public override object ToGenericObject() => Value;
        public override bool IsArray => false;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueJSONScalar);
        public bool Equals([AllowNull] AttributeValueJSONScalar other) => other != null && JToken.DeepEquals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();
        public override bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            return Value.FullTextSearch(searchString, compareOptions);
        }

        internal static AttributeValueJSONScalar Build(string value)
        {
            var v = JToken.Parse(value); // TODO: throws JsonReaderException, handle, but how?
            return Build(v);
        }

        public static AttributeValueJSONScalar Build(JToken value)
        {
            var n = new AttributeValueJSONScalar
            {
                Value = value
            };
            return n;
        }

    }

    public class AttributeValueJSONArray : AttributeValueJSON, IEquatable<AttributeValueJSONArray>
    {
        public JToken[] Values { get; private set; }
        public override string Value2String() => string.Join(",", Values.Select(value => value.ToString().Replace(",", "\\,")));
        public override AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Values.Select(v => v.ToString()).ToArray(), Type);
        public override object ToGenericObject() => Values;
        public override bool IsArray => true;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueJSONArray);

        private class EqualityComparer : IEqualityComparer<JToken>
        {
            public bool Equals(JToken x, JToken y) => JToken.DeepEquals(x, y);
            public int GetHashCode(JToken obj) => obj.GetHashCode();
        }
        private static readonly EqualityComparer ec = new EqualityComparer();
        public bool Equals([AllowNull] AttributeValueJSONArray other) 
            => other != null && Values.SequenceEqual(other.Values, ec);
        public override int GetHashCode() => Values.GetHashCode();
        public override bool FullTextSearch(string searchString, CompareOptions compareOptions) 
            => Values.Any(value => value.FullTextSearch(searchString, compareOptions));


        public static AttributeValueJSONArray Build(string[] values)
        {
            var jsonValues = values.Select(value => { return JToken.Parse(value); }).ToArray(); // TODO: throws JsonReaderException, handle, but how?
            return Build(jsonValues);
        }

        public static AttributeValueJSONArray Build(JToken[] values)
        {
            var n = new AttributeValueJSONArray
            {
                Values = values
            };
            return n;
        }
    }
}
