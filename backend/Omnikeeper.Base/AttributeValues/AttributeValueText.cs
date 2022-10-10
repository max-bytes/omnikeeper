using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Entity.AttributeValues
{
    public interface IAttributeValueText
    {
        IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
    }

    public sealed record class AttributeScalarValueText(string Value, bool Multiline = false) : IAttributeScalarValue<string>, IAttributeValueText
    {
        public string Value2String() => Value;
        public string[] ToRawDTOValues() => new string[] { Value };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public override string ToString() => $"AV-Text: {Value2String()}";

        public AttributeValueType Type => (Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            return ApplyTextLengthConstraint(Value, minimum, maximum);
        }
        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            return MatchRegex(Value, regex);
        }

        public static IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(string value, int? minimum, int? maximum)
        {
            if (maximum.HasValue && value.Length > maximum)
                yield return new TemplateErrorAttributeGeneric("Text too long!");
            else if (minimum.HasValue && value.Length < minimum)
                yield return new TemplateErrorAttributeGeneric("Text too short!");
        }
        public static IEnumerable<ITemplateErrorAttribute> MatchRegex(string value, Regex regex)
        {
            var match = regex.Match(value);
            if (!match.Success)
                yield return new TemplateErrorAttributeGeneric($"Regex {regex} did not match text {value}");
        }
    }

    public sealed record class AttributeArrayValueText(string[] Values, bool Multiline) : IAttributeArrayValue, IAttributeValueText
    {
        public AttributeValueType Type => Multiline ? AttributeValueType.MultilineText : AttributeValueType.Text;

        public int Length => Values.Length;
        public bool IsArray => true;

        public static AttributeArrayValueText BuildFromString(IEnumerable<string> values, bool multiline = false)
        {
            return new AttributeArrayValueText(values.ToArray(), multiline);
        }

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in AttributeScalarValueText.ApplyTextLengthConstraint(Values[i], minimum, maximum)) yield return e;
            }
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in AttributeScalarValueText.MatchRegex(Values[i], regex)) yield return e;
            }
        }

        public bool Equals(AttributeArrayValueText? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values;
        public object ToGenericObject() => Values;
        public object ToGraphQLValue() => Values;
        public string Value2String() => string.Join(",", Values.Select(value => value.Replace(",", "\\,")));
    }
}
