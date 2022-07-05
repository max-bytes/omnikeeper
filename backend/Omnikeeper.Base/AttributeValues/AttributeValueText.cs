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

        public bool Equals(IAttributeValue? other) => Equals(other as AttributeScalarValueText);

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
    }

    public sealed record class AttributeArrayValueText(AttributeScalarValueText[] Values) : AttributeArrayValue<AttributeScalarValueText, string>(Values), IAttributeValueText
    {
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

    }
}
