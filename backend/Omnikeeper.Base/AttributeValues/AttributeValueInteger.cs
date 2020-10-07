using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeValueIntegerScalar : IAttributeScalarValue<long>, IEquatable<AttributeValueIntegerScalar>
    {
        public long Value { get; private set; }
        public string Value2String() => Value.ToString();
        public AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Value.ToString(), Type);
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Integer: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Integer;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueIntegerScalar);
        public bool Equals([AllowNull] AttributeValueIntegerScalar other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        { // does not make sense for integer
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        { // does not make sense for integer
            yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, Type);
        }

        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(Value.ToString(), searchString, compareOptions) >= 0;

        public static AttributeValueIntegerScalar Build(long value)
        {
            return new AttributeValueIntegerScalar
            {
                Value = value
            };
        }

        public static AttributeValueIntegerScalar Build(string value)
        {
            long.TryParse(value, out var v);
            return Build(v);
        }
    }


    public class AttributeValueIntegerArray : AttributeArrayValue<AttributeValueIntegerScalar, long>
    {
        public override AttributeValueType Type => AttributeValueType.Integer;

        public static AttributeValueIntegerArray Build(long[] values)
        {
            return new AttributeValueIntegerArray()
            {
                Values = values.Select(v => AttributeValueIntegerScalar.Build(v)).ToArray()
            };
        }

        public static AttributeValueIntegerArray Build(string[] values)
        {
            var longValues = values.Select(value => { long.TryParse(value, out var v); return v; }).ToArray();
            return Build(longValues);
        }
    }
}
