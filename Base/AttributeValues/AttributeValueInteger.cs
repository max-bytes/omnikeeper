using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.AttributeValues
{
    public class AttributeValueInteger : IAttributeValue, IEquatable<AttributeValueInteger>
    {
        public long Value { get; private set; }

        internal static IAttributeValue Build(string value)
        {
            long.TryParse(value, out var v);
            return Build(v);
        }

        public static IAttributeValue Build(long value)
        {
            var n = new AttributeValueInteger
            {
                Value = value
            };
            return n;
        }

        public string Value2String() => Value.ToString();

        public override string ToString()
        {
            return $"AV-Integer: {Value}";
        }

        public AttributeValueGenericScalar ToGeneric() => AttributeValueGenericScalar.Build(Value2String(), Type);
        public AttributeValueType Type => AttributeValueType.Integer;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueInteger);
        public bool Equals([AllowNull] AttributeValueInteger other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
