using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.Template
{
    public interface ITemplateErrorAttribute
    {
        string ErrorMessage { get; }
    }
    public class TemplateErrorAttributeMissing : ITemplateErrorAttribute
    {
        public string AttributeName { get; private set; }
        public AttributeValueType? Type { get; private set; }

        public string ErrorMessage => $"attribute \"{AttributeName}\" {((Type.HasValue) ? $" of type \"{Type.Value}\" " : "")}is missing!";

        public static TemplateErrorAttributeMissing Build(string attributeName, AttributeValueType? type)
        {
            return new TemplateErrorAttributeMissing()
            {
                AttributeName = attributeName,
                Type = type
            };
        }
    }
    public class TemplateErrorAttributeGeneric : ITemplateErrorAttribute
    {
        public string ErrorMessage { get; private set; }

        public static TemplateErrorAttributeGeneric Build(string message)
        {
            return new TemplateErrorAttributeGeneric()
            {
                ErrorMessage = message
            };
        }
    }
    public class TemplateErrorAttributeWrongType : ITemplateErrorAttribute
    {
        public string ErrorMessage => $"attribute must be (one) of type \"{string.Join(", ", CorrectTypes)}\", is type \"{CurrentType}\"!";
        public AttributeValueType[] CorrectTypes { get; private set; }
        public AttributeValueType CurrentType { get; private set; }

        public static TemplateErrorAttributeWrongType Build(AttributeValueType correctType, AttributeValueType currentType)
        {
            return Build(new AttributeValueType[] { correctType }, currentType);
        }
        public static TemplateErrorAttributeWrongType Build(AttributeValueType[] correctTypes, AttributeValueType currentType)
        {
            return new TemplateErrorAttributeWrongType()
            {
                CorrectTypes = correctTypes,
                CurrentType = currentType
            };
        }
    }
    public class TemplateErrorAttributeWrongMultiplicity : ITemplateErrorAttribute
    {
        public string ErrorMessage => (CorrectIsArray) ? $"attribute must be array, is scalar!" : $"attribute must be scalar, is array!";
        public bool CorrectIsArray { get; private set; }

        public static TemplateErrorAttributeWrongMultiplicity Build(bool correctIsArray)
        {
            return new TemplateErrorAttributeWrongMultiplicity()
            {
                CorrectIsArray = correctIsArray
            };
        }
    }

    public class TemplateErrorsAttribute
    {
        public string AttributeName { get; private set; }
        public IEnumerable<ITemplateErrorAttribute> Errors { get; private set; }
        public static TemplateErrorsAttribute Build(string name, IEnumerable<ITemplateErrorAttribute> errors)
        {
            return new TemplateErrorsAttribute()
            {
                AttributeName = name,
                Errors = errors
            };
        }
    }
    public class TemplateErrorsCI
    {
        public IDictionary<string, TemplateErrorsAttribute> AttributeErrors { get; private set; }

        public static TemplateErrorsCI Build(IDictionary<string, TemplateErrorsAttribute> attributeErrors)
        {
            return new TemplateErrorsCI()
            {
                AttributeErrors = attributeErrors
            };
        }
    }
}
