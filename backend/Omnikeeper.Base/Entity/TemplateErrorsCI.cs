using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
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

        public TemplateErrorAttributeMissing(string attributeName, AttributeValueType? type)
        {
            AttributeName = attributeName;
            Type = type;
        }
    }
    public class TemplateErrorAttributeGeneric : ITemplateErrorAttribute
    {
        public string ErrorMessage { get; private set; }

        public TemplateErrorAttributeGeneric(string message)
        {
            ErrorMessage = message;
        }
    }
    public class TemplateErrorAttributeWrongType : ITemplateErrorAttribute
    {
        public string ErrorMessage => $"attribute must be (one) of type \"{string.Join(", ", CorrectTypes)}\", is type \"{CurrentType}\"!";
        public AttributeValueType[] CorrectTypes { get; private set; }
        public AttributeValueType CurrentType { get; private set; }

        public static TemplateErrorAttributeWrongType BuildFromSingle(AttributeValueType correctType, AttributeValueType currentType)
        {
            return new TemplateErrorAttributeWrongType(new AttributeValueType[] { correctType }, currentType);
        }
        public TemplateErrorAttributeWrongType(AttributeValueType[] correctTypes, AttributeValueType currentType)
        {
            CorrectTypes = correctTypes;
            CurrentType = currentType;
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
        public TemplateErrorsAttribute(string name, IEnumerable<ITemplateErrorAttribute> errors)
        {
            AttributeName = name;
            Errors = errors;
        }
    }



    public interface ITemplateErrorRelation
    {
        string ErrorMessage { get; }
    }
    public class TemplateErrorRelationGeneric : ITemplateErrorRelation
    {
        public string ErrorMessage { get; private set; }

        public TemplateErrorRelationGeneric(string message)
        {
            ErrorMessage = message;
        }
    }
    public class TemplateErrorsRelation
    {
        public string PredicateID { get; private set; }
        public IEnumerable<ITemplateErrorRelation> Errors { get; private set; }
        public TemplateErrorsRelation(string predicateID, IEnumerable<ITemplateErrorRelation> errors)
        {
            PredicateID = predicateID;
            Errors = errors;
        }
    }

    public class TemplateErrorsCI
    {
        public IDictionary<string, TemplateErrorsAttribute> AttributeErrors { get; private set; }
        public IDictionary<string, TemplateErrorsRelation> RelationErrors { get; private set; }

        public TemplateErrorsCI(IDictionary<string, TemplateErrorsAttribute> attributeErrors, IDictionary<string, TemplateErrorsRelation> relationErrors)
        {
            AttributeErrors = attributeErrors;
            RelationErrors = relationErrors;
        }
    }
}
