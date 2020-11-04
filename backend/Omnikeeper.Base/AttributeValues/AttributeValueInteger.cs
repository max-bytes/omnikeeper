using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueInteger : IAttributeScalarValue<long>, IEquatable<AttributeScalarValueInteger>
    {
        public long Value { get; private set; }
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues() => new string[] { Value.ToString() };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Integer: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Integer;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueInteger);
        public bool Equals([AllowNull] AttributeScalarValueInteger other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueInteger Build(long value)
        {
            return new AttributeScalarValueInteger
            {
                Value = value
            };
        }

        public static AttributeScalarValueInteger BuildFromString(string value)
        {
            long.TryParse(value, out var v);
            return Build(v);
        }

    }


    public class AttributeArrayValueInteger : AttributeArrayValue<AttributeScalarValueInteger, long>
    {
        public override AttributeValueType Type => AttributeValueType.Integer;

        public static AttributeArrayValueInteger Build(long[] values)
        {
            return new AttributeArrayValueInteger()
            {
                Values = values.Select(v => AttributeScalarValueInteger.Build(v)).ToArray()
            };
        }

        public static AttributeArrayValueInteger BuildFromString(string[] values)
        {
            var longValues = values.Select(value => { long.TryParse(value, out var v); return v; }).ToArray();
            return Build(longValues);
        }
    }
}
