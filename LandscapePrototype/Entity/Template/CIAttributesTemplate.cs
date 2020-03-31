using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.Template
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
            switch (value)
            {
                case AttributeValueText t:
                    var tooLong = Maximum.HasValue && t.Value.Length > Maximum;
                    var tooShort = Minimum.HasValue && t.Value.Length < Minimum;
                    if (tooLong)
                        yield return TemplateErrorAttributeGeneric.Build("Text too long!");
                    else if (tooShort)
                        yield return TemplateErrorAttributeGeneric.Build("Text too short!");
                    break;
                case AttributeValueInteger i:
                    yield return TemplateErrorAttributeWrongType.Build(AttributeValueType.Text, i.Type);
                    break;
                default:
                    throw new Exception("Unknown type");
            }
        }
    }

    public class CIAttributeTemplate
    {
        public string Name { get; private set; }
        public string Description { get; private set; } // TODO: use description
        public AttributeValueType? Type { get; private set; }
        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)
        public IEnumerable<ICIAttributeValueConstraint> ValueConstraints { get; private set; }

        public static CIAttributeTemplate BuildFromParams(string name, string description, AttributeValueType? type, params ICIAttributeValueConstraint[] valueConstraints)
        {
            return Build(name, description, type, valueConstraints);
        }

        public static CIAttributeTemplate Build(string name, string description, AttributeValueType? type, IEnumerable<ICIAttributeValueConstraint> valueConstraints)
        {
            return new CIAttributeTemplate()
            {
                Name = name,
                Description = description,
                Type = type,
                ValueConstraints = valueConstraints
            };
        }
    }

    public class CIAttributesTemplate
    {
        public CIType CIType { get; private set; }
        public IImmutableDictionary<string, CIAttributeTemplate> Attributes { get; private set; }

        public static CIAttributesTemplate Build(CIType ciType, IList<CIAttributeTemplate> attributes)
        {
            return new CIAttributesTemplate()
            {
                CIType = ciType,
                Attributes = attributes.ToImmutableDictionary(a => a.Name)
            };
        }
    }
}
