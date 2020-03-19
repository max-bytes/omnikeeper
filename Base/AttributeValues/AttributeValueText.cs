using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.AttributeValues
{
    public class AttributeValueText : IAttributeValue, IEquatable<AttributeValueText>
    {
        public string Value { get; private set; }

        public static IAttributeValue Build(string value)
        {
            return new AttributeValueText
            {
                Value = value
            };
        }

        public string Value2String() => Value;

        public override string ToString()
        {
            return $"AV-Text: {Value}";
        }

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueText);
        public bool Equals([AllowNull] AttributeValueText other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
