using LandscapeRegistry.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Landscape.Base.Entity
{
    public interface ICIAttributeValueConstraint
    {
        IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value);
    }

    public class CIAttributeValueConstraintTextLength : ICIAttributeValueConstraint
    {
        public int? Minimum { get; private set; }
        public int? Maximum { get; private set; }

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
            return value.ApplyTextLengthConstraint(Minimum, Maximum);
        }
    }

    public class CIAttributeValueConstraintTextRegex : ICIAttributeValueConstraint
    {
        public Regex Regex { get; private set; }
        public static CIAttributeValueConstraintTextRegex Build(Regex regex)
        {
            return new CIAttributeValueConstraintTextRegex()
            {
                Regex = regex
            };
        }

        public IEnumerable<ITemplateErrorAttribute> CalculateErrors(IAttributeValue value)
        {
            return value.MatchRegex(Regex);
        }
    }

    public class CIAttributeTemplate
    {
        public string Name { get; private set; }
        // TODO: descriptions
        public AttributeValueType? Type { get; private set; } // TODO: could be more than one type allowed
        public bool? IsArray { get; private set; }
        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)
        public IEnumerable<ICIAttributeValueConstraint> ValueConstraints { get; private set; }

        public static CIAttributeTemplate BuildFromParams(string name, AttributeValueType? type, bool? isArray, params ICIAttributeValueConstraint[] valueConstraints)
        {
            return Build(name, type, isArray, valueConstraints);
        }

        public static CIAttributeTemplate Build(string name, AttributeValueType? type, bool? isArray, IEnumerable<ICIAttributeValueConstraint> valueConstraints)
        {
            return new CIAttributeTemplate()
            {
                Name = name,
                Type = type,
                IsArray = isArray,
                ValueConstraints = valueConstraints
            };
        }
    }
}
