using JsonSubTypes;
using Newtonsoft.Json;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Entity
{
    [JsonConverter(typeof(JsonSubtypes), "type")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextRegex), "textRegex")]
    [JsonSubtypes.KnownSubType(typeof(CIAttributeValueConstraintTextLength), "textLength")]
    public interface ICIAttributeValueConstraint
    {
        public string type { get; }
        IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value);
    }

    [Serializable]
    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint
    {
        public readonly int? Minimum;
        public readonly int? Maximum;

        public CIAttributeValueConstraintTextLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        [JsonIgnore]
        public string type => "textLength";

        public static CIAttributeValueConstraintTextLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintTextLength(min, max);
        }

        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                return v.ApplyTextLengthConstraint(Minimum, Maximum);
            }
            else
            {
                return new ITemplateErrorAttribute[] { new TemplateErrorAttributeWrongType(new AttributeValueType[] { AttributeValueType.Text, AttributeValueType.MultilineText }, value.Type) };
            }
        }
    }

    [Serializable]
    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint
    {
        public readonly string RegexStr;
        public readonly RegexOptions RegexOptions;

        [JsonIgnore]
        public string type => "textRegex";

        [JsonIgnore]
        [NonSerialized]
        private Regex? regex;

        public CIAttributeValueConstraintTextRegex(Regex r)
        {
            RegexStr = r.ToString(); // NOTE: weird, but ToString() returns the original pattern
            RegexOptions = r.Options;
            regex = r;
        }

        [JsonConstructor]
        public CIAttributeValueConstraintTextRegex(string regexStr, RegexOptions regexOptions)
        {
            RegexStr = regexStr;
            RegexOptions = regexOptions;
            regex = null;
        }

        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                if (regex == null)
                    regex = new Regex(RegexStr, RegexOptions);
                return v.MatchRegex(regex);
            }
            else
            {
                return new ITemplateErrorAttribute[] { new TemplateErrorAttributeWrongType(new AttributeValueType[] { AttributeValueType.Text, AttributeValueType.MultilineText }, value.Type) };
            }
        }
    }

    [Serializable]
    public class CIAttributeTemplate
    {
        public readonly string Name;
        // TODO: descriptions
        public readonly AttributeValueType? Type; // TODO: could be more than one type allowed
        public readonly bool? IsArray;
        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)
        public readonly IEnumerable<ICIAttributeValueConstraint> ValueConstraints;

        public static CIAttributeTemplate BuildFromParams(string name, AttributeValueType? type, bool? isArray, params ICIAttributeValueConstraint[] valueConstraints)
        {
            return new CIAttributeTemplate(name, type, isArray, valueConstraints);
        }

        public CIAttributeTemplate(string name, AttributeValueType? type, bool? isArray, IEnumerable<ICIAttributeValueConstraint> valueConstraints)
        {
            Name = name;
            Type = type;
            IsArray = isArray;
            ValueConstraints = valueConstraints;
        }
    }
}
