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
            var n = new AttributeValueInteger();
            long.TryParse(value, out var v);
            n.Value = v;
            return n;
        }

        public string Value2String() => Value.ToString();

        public override string ToString()
        {
            return $"AV-Integer: {Value}";
        }

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueInteger);
        public bool Equals([AllowNull] AttributeValueInteger other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
