using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

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

        public abstract IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        public abstract IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
        public abstract bool FullTextSearch(string searchString, CompareOptions compareOptions);

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

        public override IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            if (maximum.HasValue && Value.Length > maximum)
                yield return TemplateErrorAttributeGeneric.Build("Text too long!");
            else if (minimum.HasValue && Value.Length < minimum)
                yield return TemplateErrorAttributeGeneric.Build("Text too short!");
        }
        public override IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            var match = regex.Match(Value);
            if (!match.Success)
                yield return TemplateErrorAttributeGeneric.Build($"Regex {regex} did not match text {Value}");
        }

        public override bool FullTextSearch(string searchString, CompareOptions compareOptions) 
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(Value, searchString, compareOptions) >= 0;

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

        public override IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                var tooLong = maximum.HasValue && Values[i].Length > maximum;
                var tooShort = minimum.HasValue && Values[i].Length < minimum;
                if (tooLong)
                    yield return TemplateErrorAttributeGeneric.Build($"Text[{i}] too long!");
                else if (tooShort)
                    yield return TemplateErrorAttributeGeneric.Build($"Text[{i}] too short!");
            }
        }
        public override IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            foreach (var value in Values)
            {
                var match = regex.Match(value);
                if (!match.Success)
                    yield return TemplateErrorAttributeGeneric.Build($"Regex {regex} did not match text {value}");
            }
        }

        public override bool FullTextSearch(string searchString, CompareOptions compareOptions) 
            => Values.Any(value => CultureInfo.InvariantCulture.CompareInfo.IndexOf(value, searchString, compareOptions) >= 0);
    }
}
