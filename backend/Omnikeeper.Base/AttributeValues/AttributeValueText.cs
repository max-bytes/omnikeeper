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
    public interface IAttributeValueText
    {
        IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
        bool FullTextSearch(string searchString, CompareOptions compareOptions);
    }

    public class AttributeScalarValueText : IAttributeScalarValue<string>, IEquatable<AttributeScalarValueText>, IAttributeValueText
    {
        public bool Multiline { get; protected set; }

        public string Value { get; private set; }
        public string Value2String() => Value;
        public string[] ToRawDTOValues() => new string[] { Value };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Text: {Value2String()}";

        public AttributeValueType Type => (Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueText);
        public bool Equals([AllowNull] AttributeScalarValueText other) => other != null && Value == other.Value && Multiline == other.Multiline;
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueText BuildFromString(string value, bool multiline = false)
        {
            return new AttributeScalarValueText
            {
                Value = value,
                Multiline = multiline
            };
        }

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            if (maximum.HasValue && Value.Length > maximum)
                yield return TemplateErrorAttributeGeneric.Build("Text too long!");
            else if (minimum.HasValue && Value.Length < minimum)
                yield return TemplateErrorAttributeGeneric.Build("Text too short!");
        }
        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            var match = regex.Match(Value);
            if (!match.Success)
                yield return TemplateErrorAttributeGeneric.Build($"Regex {regex} did not match text {Value}");
        }

        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(Value, searchString, compareOptions) >= 0;
    }


    public class AttributeArrayValueText : AttributeArrayValue<AttributeScalarValueText, string>, IAttributeValueText
    {
        public override AttributeValueType Type => Values.Any(v => v.Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public static AttributeArrayValueText BuildFromString(string[] values, bool multiline = false)
        {
            return new AttributeArrayValueText()
            {
                Values = values.Select(v => AttributeScalarValueText.BuildFromString(v, multiline)).ToArray()
            };
        }

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in Values[i].ApplyTextLengthConstraint(minimum, maximum)) yield return e;
            }
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in Values[i].MatchRegex(regex)) yield return e;
            }
        }

        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            return Values.Any(v => v.FullTextSearch(searchString, compareOptions));
        }

    }
}
