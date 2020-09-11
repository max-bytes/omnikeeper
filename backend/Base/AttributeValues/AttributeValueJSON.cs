using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public class AttributeScalarValueJSON : IAttributeScalarValue<JToken>, IEquatable<AttributeScalarValueJSON>
    {
        public static JToken ErrorValue(string message) => JToken.Parse($"{{\"error\": \"{message}\" }}");

        public override string ToString() => $"AV-JSON: {Value2String()}";

        public JToken Value { get; private set; }
        public string Value2String() => Value.ToString();
        public AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Value.ToString(), Type);
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public AttributeValueType Type => AttributeValueType.JSON;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals([AllowNull] AttributeScalarValueJSON other) => other != null && JToken.DeepEquals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for JSON
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        { // does not make sense for JSON
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            return Value.FullTextSearch(searchString, compareOptions);
        }

        public static AttributeScalarValueJSON Build(string value)
        {
            try
            {
                var v = JToken.Parse(value);
                return Build(v);
            }
            catch (JsonReaderException e)
            {
                return Build(ErrorValue(e.Message));
            }
        }

        public static AttributeScalarValueJSON Build(JToken value)
        {
            var n = new AttributeScalarValueJSON
            {
                Value = value
            };
            return n;
        }
    }


    public class AttributeArrayValueJSON : AttributeArrayValue<AttributeScalarValueJSON, JToken>
    {
        public override AttributeValueType Type => AttributeValueType.JSON;

        public static AttributeArrayValueJSON Build(string[] values)
        {
            var jsonValues = values.Select(value =>
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch (JsonReaderException e)
                {
                    return AttributeScalarValueJSON.ErrorValue(e.Message);
                }
            }).ToArray();
            return Build(jsonValues);
        }

        public static AttributeArrayValueJSON Build(JToken[] values)
        {
            var n = new AttributeArrayValueJSON
            {
                Values = values.Select(v => AttributeScalarValueJSON.Build(v)).ToArray()
            };
            return n;
        }
    }
}
