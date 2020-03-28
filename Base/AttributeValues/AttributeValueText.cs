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
        public bool Multiline { get; private set; }

        public static IAttributeValue Build(string value, bool multiline = false)
        {
            return new AttributeValueText
            {
                Value = value,
                Multiline = multiline
            };
        }

        public string Value2String() => Value;

        public override string ToString()
        {
            return $"AV-Text ({((Multiline) ? "Multiline" : "")}): {Value}";
        }

        public AttributeValueGeneric ToGeneric() => AttributeValueGeneric.Build(Value2String(), Type);
        public AttributeValueType Type => (Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueText);
        public bool Equals([AllowNull] AttributeValueText other) => other != null && Value == other.Value && Multiline == other.Multiline;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
