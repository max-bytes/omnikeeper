﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueJSON : IAttributeScalarValue<JToken>, IEquatable<AttributeScalarValueJSON>
    {
        public static JToken ErrorValue(string message) => JToken.Parse($"{{\"error\": \"{message}\" }}");

        public override string ToString() => $"AV-JSON: {Value2String()}";

        public JToken Value { get; private set; }
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues() => new string[] { Value.ToString() };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public AttributeValueType Type => AttributeValueType.JSON;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals([AllowNull] AttributeScalarValueJSON other) => other != null && JToken.DeepEquals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueJSON BuildFromString(string value)
        {
            try
            {
                var v = JToken.Parse(value);
                return new AttributeScalarValueJSON(v);
            }
            catch (JsonReaderException e)
            {
                return new AttributeScalarValueJSON(ErrorValue(e.Message));
            }
        }

        public AttributeScalarValueJSON(JToken value)
        {
            Value = value;
        }
    }


    public class AttributeArrayValueJSON : AttributeArrayValue<AttributeScalarValueJSON, JToken>
    {
        protected AttributeArrayValueJSON(AttributeScalarValueJSON[] values) : base(values)
        {
        }

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
                    return AttributeScalarValueJSON.ErrorValue(e.Message);
                }
            }).ToArray();
            return Build(jsonValues);
        }

        public static AttributeArrayValueJSON Build(JToken[] values)
        {
            var n = new AttributeArrayValueJSON(
                values.Select(v => new AttributeScalarValueJSON(v)).ToArray()
            );
            return n;
        }
    }
}
