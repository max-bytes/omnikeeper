using System;
using System.Diagnostics.CodeAnalysis;
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

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueBoolean);

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

    public sealed record class AttributeArrayValueBoolean(AttributeScalarValueBoolean[] Values) : AttributeArrayValue<AttributeScalarValueBoolean, bool>(Values)
    {
        public override AttributeValueType Type => AttributeValueType.Boolean;

        public static AttributeArrayValueBoolean Build(bool[] values)
        {
            return new AttributeArrayValueBoolean(
                values.Select(v => new AttributeScalarValueBoolean(v)).ToArray()
            );
        }

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
    }
}
