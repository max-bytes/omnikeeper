using Landscape.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Entity.AttributeValues
{

    public abstract class AttributeValueText : IAttributeValue
    {
        public bool Multiline { get; protected set; }

        public override string ToString() => $"AV-Text ({((Multiline) ? "Multiline" : "")}): {Value2String()}";

        public AttributeValueType Type => (Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public abstract string Value2String();
        public abstract bool IsArray { get; }
        public abstract AttributeValueDTO ToGeneric();
        public abstract bool Equals(IAttributeValue other);
    }

    public class AttributeValueTextScalar : AttributeValueText, IEquatable<AttributeValueTextScalar>
    {
        public string Value { get; private set; }
        public override string Value2String() => Value;
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Value, Type);
        public override bool IsArray => false;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueTextScalar);
        public bool Equals([AllowNull] AttributeValueTextScalar other) => other != null && Value == other.Value && Multiline == other.Multiline;
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeValueTextScalar Build(string value, bool multiline = false)
        {
            return new AttributeValueTextScalar
            {
                Value = value,
                Multiline = multiline
            };
        }

    }

    public class AttributeValueTextArray : AttributeValueText, IEquatable<AttributeValueTextArray>
    {
        public string[] Values { get; private set; }
        public override string Value2String() => string.Join(",", Values.Select(value => value.Replace(",", "\\,")));
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Values, Type);
        public override bool IsArray => true;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueTextArray);
        public bool Equals([AllowNull] AttributeValueTextArray other) => other != null && Values.SequenceEqual(other.Values) && Multiline == other.Multiline;
        public override int GetHashCode() => Values.GetHashCode();

        public static AttributeValueTextArray Build(string[] values, bool multiline = false)
        {
            return new AttributeValueTextArray
            {
                Values = values,
                Multiline = multiline
            };
        }

    }
}
