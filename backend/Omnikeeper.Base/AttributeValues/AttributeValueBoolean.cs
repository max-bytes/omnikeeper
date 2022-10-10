using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueBoolean(bool Value) : IAttributeScalarValue<bool>
    {
        public string Value2String() => Value ? "true" : "false";
        public string[] ToRawDTOValues() => new string[] { Value ? "true" : "false" };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override string ToString() => $"AV-Boolean: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Boolean;

        //public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueBoolean);

        public static AttributeScalarValueBoolean BuildFromBytes(byte[] value)
        {
            var v = BitConverter.ToBoolean(value, 0);
            return new AttributeScalarValueBoolean(v);
        }

        public static AttributeScalarValueBoolean BuildFromString(string value)
        {
            if (bool.TryParse(value, out var v))
                return new AttributeScalarValueBoolean(v);
            else
                throw new Exception("Could not parse boolean for attribute value");
        }
    }

    public sealed record class AttributeArrayValueBoolean(bool[] Values) : IAttributeArrayValue
    {
        public AttributeValueType Type => AttributeValueType.Boolean;

        public int Length => Values.Length;
        public bool IsArray => true;

        public static AttributeArrayValueBoolean Build(bool[] values)
        {
            return new AttributeArrayValueBoolean(values);
        }

        public IEnumerable<byte[]> ToBytes() => Values.Select(v => BitConverter.GetBytes(v));

        // NOTE: assumes booleans are packed one after the other
        internal static IAttributeValue BuildFromBytes(byte[] bytes)
        {
            var num = bytes.Length / 1;
            var arr = new bool[num];
            for (var i = 0; i < num; i++)
            {
                var v = BitConverter.ToBoolean(bytes, i * 1);
                arr[i] = v;
            }
            return Build(arr);
        }
        public static AttributeArrayValueBoolean BuildFromString(string[] values)
        {
            var boolValues = values.Select(value =>
            {
                if (bool.TryParse(value, out var v))
                    return v;
                else
                    throw new Exception("Could not parse boolean for attribute value");
            }).ToArray();
            return Build(boolValues);
        }

        public bool Equals(AttributeArrayValueBoolean? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values.Select(v => v ? "true" : "false").ToArray();
        public object ToGenericObject() => Values;
        public object ToGraphQLValue() => Values;
        public string Value2String() => string.Join(",", Values.Select(value => value ? "true" : "false"));
    }
}
