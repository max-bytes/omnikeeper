using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueInteger(long Value) : IAttributeScalarValue<long>
    {
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues() => new string[] { Value.ToString() };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public override string ToString() => $"AV-Integer: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Integer;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueInteger);

        public static AttributeScalarValueInteger BuildFromString(string value)
        {
            if (long.TryParse(value, out var v))
                return new AttributeScalarValueInteger(v);
            else
                throw new Exception("Could not parse integer for attribute value");
        }

    }

    public sealed record class AttributeArrayValueInteger(AttributeScalarValueInteger[] Values) : AttributeArrayValue<AttributeScalarValueInteger, long>(Values)
    {
        public override AttributeValueType Type => AttributeValueType.Integer;

        public static AttributeArrayValueInteger Build(long[] values)
        {
            return new AttributeArrayValueInteger(
                values.Select(v => new AttributeScalarValueInteger(v)).ToArray()
            );
        }

        public static AttributeArrayValueInteger BuildFromString(string[] values)
        {
            var longValues = values.Select(value =>
            {
                if (long.TryParse(value, out var v))
                    return v;
                else
                    throw new Exception("Could not parse integer for attribute value");
            }).ToArray();
            return Build(longValues);
        }
    }
}
