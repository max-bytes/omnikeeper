using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueJSON : IAttributeScalarValue<JToken>, IEquatable<AttributeScalarValueJSON>
    {
        public static JToken ErrorValue(string message) => JToken.Parse($"{{\"error\": \"{message}\" }}");

        public override string ToString() => $"AV-JSON: {Value2String()}";

        private readonly JToken value;
        public JToken Value => value;
        private readonly string valueStr;
        public string ValueStr => valueStr;

        public string Value2String() => valueStr;
        public string[] ToRawDTOValues() => new string[] { valueStr };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => valueStr;

        public AttributeValueType Type => AttributeValueType.JSON;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals([AllowNull] AttributeScalarValueJSON other) => other != null && JToken.DeepEquals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueJSON BuildFromString(string value)
        {
            try
            {
                var v = JToken.Parse(value);
                return new AttributeScalarValueJSON(v, v.ToString());
            }
            catch (JsonReaderException e)
            {
                throw new Exception("Error building JSON attribute value from string", e);
            }
        }

        public static AttributeScalarValueJSON Build(JToken value)
        {
            return new AttributeScalarValueJSON(value, value.ToString());
        }

        private AttributeScalarValueJSON(JToken value, string valueStr)
        {
            this.value = value;
            this.valueStr = valueStr;
        }
    }

    public class AttributeArrayValueJSON : AttributeArrayValue<AttributeScalarValueJSON, JToken>
    {
        protected AttributeArrayValueJSON(AttributeScalarValueJSON[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueJSON() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => AttributeValueType.JSON;

        public static AttributeArrayValueJSON BuildFromString(string[] values)
        {
            var jsonValues = values.Select(value =>
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch (JsonReaderException e)
                {
                    throw new Exception("Error building JSON attribute value from string", e);
                }
            }).ToArray();
            return Build(jsonValues);
        }

        public static AttributeArrayValueJSON Build(IEnumerable<JToken> values)
        {
            var n = new AttributeArrayValueJSON(
                values.Select(v => AttributeScalarValueJSON.Build(v)).ToArray()
            );
            return n;
        }
    }
}
