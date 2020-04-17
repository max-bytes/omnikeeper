using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public abstract class AttributeValueInteger : IAttributeValue
    {
        public override string ToString() => $"AV-Integer: {Value2String()}";
        public AttributeValueType Type => AttributeValueType.Integer;
        public abstract string Value2String();
        public abstract bool IsArray { get; }
        public abstract AttributeValueDTO ToGeneric();
        public abstract bool Equals(IAttributeValue other);

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for integer
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }
    }

    public class AttributeValueIntegerScalar : AttributeValueInteger, IEquatable<AttributeValueIntegerScalar>
    {
        public long Value { get; private set; }
        public override string Value2String() => Value.ToString();
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Value2String(), Type);
        public override bool IsArray => false;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueIntegerScalar);
        public bool Equals([AllowNull] AttributeValueIntegerScalar other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        internal static AttributeValueIntegerScalar Build(string value)
        {
            long.TryParse(value, out var v);
            return Build(v);
        }

        public static AttributeValueIntegerScalar Build(long value)
        {
            var n = new AttributeValueIntegerScalar
            {
                Value = value
            };
            return n;
        }
    }

    public class AttributeValueIntegerArray : AttributeValueInteger, IEquatable<AttributeValueIntegerArray>
    {
        public long[] Values { get; private set; }
        public override string Value2String() => string.Join(",", Values.Select(value => value.ToString().Replace(",", "\\,")));
        public override AttributeValueDTO ToGeneric() => AttributeValueDTO.Build(Values.Select(v => v.ToString()).ToArray(), Type);
        public override bool IsArray => true;
        public override bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueIntegerArray);
        public bool Equals([AllowNull] AttributeValueIntegerArray other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public static AttributeValueIntegerArray Build(string[] values)
        {
            var longValues = values.Select(value => { long.TryParse(value, out var v); return v; }).ToArray();
            return Build(longValues);
        }

        public static AttributeValueIntegerArray Build(long[] values)
        {
            var n = new AttributeValueIntegerArray
            {
                Values = values
            };
            return n;
        }
    }
}
