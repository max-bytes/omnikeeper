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

    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint
    {
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }

        public string type => "textLength";

        public static CIAttributeValueConstraintTextLength Build(int? min, int? max)
        {
            if (min > max) throw new Exception("Minimum value must not be larger than maximum value");
            return new CIAttributeValueConstraintTextLength()
            {
                Minimum = min,
                Maximum = max
            };
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

    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint
    {
        public Regex Regex { get; set; }

        public string type => "textRegex";

        public CIAttributeValueConstraintTextRegex(Regex regex)
        {
            Regex = regex;
        }

        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            // HACK: this is a bit unclean, as we do CLR type-checking, but return an error based on the AttributeValueType value
            if (value is IAttributeValueText v)
            {
                return v.MatchRegex(Regex);
            }
            else
            {
                return new ITemplateErrorAttribute[] { new TemplateErrorAttributeWrongType(new AttributeValueType[] { AttributeValueType.Text, AttributeValueType.MultilineText }, value.Type) };
            }
        }
    }

    public class CIAttributeTemplate
    {
        public string Name { get; set; }
        // TODO: descriptions
        public AttributeValueType? Type { get; set; } // TODO: could be more than one type allowed
        public bool? IsArray { get; set; }
        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)
        public IEnumerable<ICIAttributeValueConstraint> ValueConstraints { get; set; }

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
