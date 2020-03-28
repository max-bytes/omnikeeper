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
        public string ErrorMessage { get; private set; }
        public AttributeValueType? Type { get; private set; }

        public static TemplateErrorAttributeMissing Build(string error, AttributeValueType? type)
        {
            return new TemplateErrorAttributeMissing()
            {
                ErrorMessage = error,
                Type = type
            };
        }
    }
    public class TemplateErrorAttributeWrongType : ITemplateErrorAttribute
    {
        public string ErrorMessage { get; private set; }
        public AttributeValueType CorrectType { get; private set; }

        public static TemplateErrorAttributeWrongType Build(string error, AttributeValueType correctType)
        {
            return new TemplateErrorAttributeWrongType()
            {
                ErrorMessage = error,
                CorrectType = correctType
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
