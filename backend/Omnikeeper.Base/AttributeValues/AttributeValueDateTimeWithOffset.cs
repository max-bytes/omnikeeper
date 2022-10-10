using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueDateTimeWithOffset(DateTimeOffset Value) : IAttributeScalarValue<DateTimeOffset>
    {
        public string Value2String() => Value.ToString("o"); // example 2009-06-15T13:45:30.0000000-07:00
        public string[] ToRawDTOValues() => new string[] { Value.ToString("o") };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public byte[] ToBytes() => Value.GetBytes();

        public override string ToString() => $"AV-DateTimeWithOffset: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.DateTimeWithOffset;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueDateTimeWithOffset);

        public static AttributeScalarValueDateTimeWithOffset BuildFromBytes(byte[] value)
        {
            var v = DateTimeOffsetExtensions.FromBytes(value, 0);
            return new AttributeScalarValueDateTimeWithOffset(v);
        }

        public static AttributeScalarValueDateTimeWithOffset BuildFromString(string value)
        {
            if (DateTimeOffset.TryParse(value, null, System.Globalization.DateTimeStyles.AssumeLocal, out var v))
                return new AttributeScalarValueDateTimeWithOffset(v);
            else
                throw new Exception("Could not parse DateTimeWithOffset for attribute value");
        }
    }

    public sealed record class AttributeArrayValueDateTimeWithOffset(DateTimeOffset[] Values) : IAttributeArrayValue
    {
        public AttributeValueType Type => AttributeValueType.DateTimeWithOffset;

        public int Length => Values.Length;
        public bool IsArray => true;

        public static AttributeArrayValueDateTimeWithOffset Build(DateTimeOffset[] values)
        {
            return new AttributeArrayValueDateTimeWithOffset(values);
        }

        public IEnumerable<byte[]> ToBytes() => Values.Select(v => v.GetBytes());

        // NOTE: assumes elements are packed one after the other
        internal static IAttributeValue BuildFromBytes(byte[] bytes)
        {
            var num = bytes.Length / 10;
            var arr = new DateTimeOffset[num];
            for (var i = 0; i < num; i++)
            {
                var v = DateTimeOffsetExtensions.FromBytes(bytes, i * 10);
                arr[i] = v;
            }
            return Build(arr);
        }
        public static AttributeArrayValueDateTimeWithOffset BuildFromString(string[] values)
        {
            var elementValues = values.Select(value =>
            {
                if (DateTimeOffset.TryParse(value, null, System.Globalization.DateTimeStyles.AssumeLocal, out var v))
                    return v;
                else
                    throw new Exception("Could not parse DateTimeWithOffset for attribute value");
            }).ToArray();
            return Build(elementValues);
        }

        public bool Equals(AttributeArrayValueDateTimeWithOffset? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values.Select(v => v.ToString("o")).ToArray();
        public object ToGenericObject() => Values;
        public object ToGraphQLValue() => Values;
        public string Value2String() => string.Join(",", Values.Select(value => value.ToString("o")));
    }
}
