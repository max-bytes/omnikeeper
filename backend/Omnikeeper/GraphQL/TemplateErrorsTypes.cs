using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL
{
    public class TemplateErrorAttributeMissingType : ObjectGraphType<TemplateErrorAttributeMissing>
    {
        public TemplateErrorAttributeMissingType()
        {
            Field(x => x.ErrorMessage);
            Field(x => x.Type, nullable: true, type: typeof(AttributeValueTypeType));
        }
    }
    public class TemplateErrorAttributeWrongTypeType : ObjectGraphType<TemplateErrorAttributeWrongType>
    {
        public TemplateErrorAttributeWrongTypeType()
        {
            Field(x => x.ErrorMessage);
            Field(x => x.CorrectTypes, type: typeof(ListGraphType<AttributeValueTypeType>));
            Field(x => x.CurrentType, type: typeof(AttributeValueTypeType));
        }
    }
    public class TemplateErrorAttributeWrongMultiplicityType : ObjectGraphType<TemplateErrorAttributeWrongMultiplicity>
    {
        public TemplateErrorAttributeWrongMultiplicityType()
        {
            Field(x => x.ErrorMessage);
            Field(x => x.CorrectIsArray);
        }
    }

    public class TemplateErrorAttributeGenericType : ObjectGraphType<TemplateErrorAttributeGeneric>
    {
        public TemplateErrorAttributeGenericType()
        {
            Field(x => x.ErrorMessage);
        }
    }
    public class TemplateErrorAttributeType : UnionGraphType
    {
        public TemplateErrorAttributeType()
        {
            Type<TemplateErrorAttributeMissingType>();
            Type<TemplateErrorAttributeWrongTypeType>();
            Type<TemplateErrorAttributeWrongMultiplicityType>();
            Type<TemplateErrorAttributeGenericType>();
        }
    }

    public class TemplateErrorsAttributeType : ObjectGraphType<TemplateErrorsAttribute>
    {
        public TemplateErrorsAttributeType()
        {
            Field(x => x.AttributeName);
            Field(x => x.Errors, type: typeof(ListGraphType<TemplateErrorAttributeType>));
        }
    }


    public class TemplateErrorRelationGenericType : ObjectGraphType<TemplateErrorRelationGeneric>
    {
        public TemplateErrorRelationGenericType()
        {
            Field(x => x.ErrorMessage);
        }
    }
    public class TemplateErrorRelationType : UnionGraphType
    {
        public TemplateErrorRelationType()
        {
            Type<TemplateErrorRelationGenericType>();
        }
    }
    public class TemplateErrorsRelationType : ObjectGraphType<TemplateErrorsRelation>
    {
        public TemplateErrorsRelationType()
        {
            Field(x => x.PredicateID);
            Field(x => x.Errors, type: typeof(ListGraphType<TemplateErrorRelationType>));
        }
    }

    public class TemplateErrorsCIType : ObjectGraphType<TemplateErrorsCI>
    {
        public TemplateErrorsCIType()
        {
            Field("attributeErrors", x => x.AttributeErrors.Values, type: typeof(ListGraphType<TemplateErrorsAttributeType>)); // GraphQL cannot deal with dictionaries, make list of values instead
            Field("relationErrors", x => x.RelationErrors.Values, type: typeof(ListGraphType<TemplateErrorsRelationType>)); // GraphQL cannot deal with dictionaries, make list of values instead
        }
    }
}
