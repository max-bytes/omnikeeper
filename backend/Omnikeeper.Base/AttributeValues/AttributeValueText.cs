using Omnikeeper.Base.Entity;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Entity.AttributeValues
{
    public interface IAttributeValueText
    {
        IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
        bool FullTextSearch(string searchString, CompareOptions compareOptions); // TODO: remove, not needed
    }

    [ProtoContract(SkipConstructor = true)]
    public class AttributeScalarValueText : IAttributeScalarValue<string>, IEquatable<AttributeScalarValueText>, IAttributeValueText
    {
        [ProtoMember(1)] private readonly bool multiline;
        public bool Multiline => multiline;
        [ProtoMember(2)] private readonly string value;
        public string Value => value;

        public string Value2String() => Value;
        public string[] ToRawDTOValues() => new string[] { Value };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Text: {Value2String()}";

        public AttributeValueType Type => (Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public bool Equals(IAttributeValue? other) => Equals(other as AttributeScalarValueText);
        public bool Equals(AttributeScalarValueText? other) => other != null && Value == other.Value && Multiline == other.Multiline;
        public override int GetHashCode() => Value.GetHashCode();

        public AttributeScalarValueText(string value, bool multiline = false)
        {
            this.value = value;
            this.multiline = multiline;
        }

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            if (maximum.HasValue && Value.Length > maximum)
                yield return new TemplateErrorAttributeGeneric("Text too long!");
            else if (minimum.HasValue && Value.Length < minimum)
                yield return new TemplateErrorAttributeGeneric("Text too short!");
        }
        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            var match = regex.Match(Value);
            if (!match.Success)
                yield return new TemplateErrorAttributeGeneric($"Regex {regex} did not match text {Value}");
        }

        // TODO: not needed, remove
        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(Value, searchString, compareOptions) >= 0;
    }

    [ProtoContract]
    public class AttributeArrayValueText : AttributeArrayValue<AttributeScalarValueText, string>, IAttributeValueText
    {
        protected AttributeArrayValueText(AttributeScalarValueText[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueText() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => Values.Any(v => v.Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public static AttributeArrayValueText BuildFromString(IEnumerable<string> values, bool multiline = false)
        {
            return new AttributeArrayValueText
            (
                values.Select(v => new AttributeScalarValueText(v, multiline)).ToArray()
            );
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
