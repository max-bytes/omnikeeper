﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueDouble(double Value) : IAttributeScalarValue<double>
    {
        public string Value2String() => Value.ToString(CultureInfo.InvariantCulture);
        public string[] ToRawDTOValues() => new string[] { Value.ToString(CultureInfo.InvariantCulture) };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override string ToString() => $"AV-Double: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Double;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueDouble);

        public static AttributeScalarValueDouble BuildFromBytes(byte[] value)
        {
            var v = BitConverter.ToDouble(value, 0);
            return new AttributeScalarValueDouble(v);
        }

        public static AttributeScalarValueDouble BuildFromString(string value)
        {
            if (double.TryParse(value, NumberStyle, CultureInfo.InvariantCulture, out var v))
                return new AttributeScalarValueDouble(v);
            else
                throw new Exception("Could not parse double for attribute value");
        }

        public const NumberStyles NumberStyle = NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowTrailingWhite |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent;
    }

    public sealed record class AttributeArrayValueDouble(double[] Values) : IAttributeArrayValue
    {
        public AttributeValueType Type => AttributeValueType.Double;

        public int Length => Values.Length;
        public bool IsArray => true;

        public static AttributeArrayValueDouble Build(double[] values)
        {
            return new AttributeArrayValueDouble(values);
        }

        public IEnumerable<byte[]> ToBytes() => Values.Select(v => BitConverter.GetBytes(v));

        // NOTE: assumes doubles are packed one after the other
        internal static IAttributeValue BuildFromBytes(byte[] bytes)
        {
            var num = bytes.Length / 8;
            var arr = new double[num];
            for (var i = 0; i < num; i++)
            {
                var v = BitConverter.ToDouble(bytes, i * 8);
                arr[i] = v;
            }
            return Build(arr);
        }
        public static AttributeArrayValueDouble BuildFromString(string[] values)
        {
            var doubleValues = values.Select(value =>
            {
                if (double.TryParse(value, AttributeScalarValueDouble.NumberStyle, CultureInfo.InvariantCulture, out var v))
                    return v;
                else
                    throw new Exception("Could not parse double for attribute value");
            }).ToArray();
            return Build(doubleValues);
        }

        public bool Equals(AttributeArrayValueDouble? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray();
        public object ToGenericObject() => Values;
        public object ToGraphQLValue() => Values;
        public string Value2String() => string.Join(",", Values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
    }
}
