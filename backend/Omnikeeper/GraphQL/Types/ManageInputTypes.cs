#nullable disable // TODO

using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL.Types
{
    public class UpsertLayerDataInput
    {
        public string ID { get; private set; }
        public string Description { get; private set; }
        public AnchorState State { get; private set; }
        public string CLConfigID { get; private set; }
        public int Color { get; private set; }
        public string[] Generators { get; private set; }
    }
    public class UpsertLayerInputDataType : InputObjectGraphType<UpsertLayerDataInput>
    {
        public UpsertLayerInputDataType()
        {
            Field("id", x => x.ID);
            Field(x => x.Description);
            Field(x => x.State, type: typeof(AnchorStateType));
            Field("clConfigID", x => x.CLConfigID, nullable: true);
            Field(x => x.Color);
            Field(x => x.Generators);
        }
    }

    public class UpsertPredicateInput
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; } = "";
        public string WordingTo { get; private set; }
    }
    public class UpsertPredicateInputType : InputObjectGraphType<UpsertPredicateInput>
    {
        public UpsertPredicateInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
        }
    }

    public class UpsertRecursiveTraitInput
    {
        public string ID { get; private set; }
        public TraitAttribute[] RequiredAttributes { get; private set; }
        public TraitAttribute[] OptionalAttributes { get; private set; }
        public TraitRelation[] OptionalRelations { get; private set; }
        public string[] RequiredTraits { get; private set; }
    }
    public class UpsertRecursiveTraitInputType : InputObjectGraphType<UpsertRecursiveTraitInput>
    {
        public UpsertRecursiveTraitInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.RequiredAttributes, type: typeof(ListGraphType<TraitAttributeInputType>));
            Field(x => x.OptionalAttributes, type: typeof(ListGraphType<TraitAttributeInputType>));
            Field(x => x.OptionalRelations, type: typeof(ListGraphType<TraitRelationInputType>));
            Field(x => x.RequiredTraits);
        }
    }

    public class TraitAttributeInputType : InputObjectGraphType<TraitAttribute>
    {
        public TraitAttributeInputType()
        {
            Field("identifier", x => x.Identifier);
            Field("template", x => x.AttributeTemplate, type: typeof(CIAttributeTemplateInputType));
        }
    }

    public class CIAttributeTemplateInputType : InputObjectGraphType<CIAttributeTemplate>
    {
        public CIAttributeTemplateInputType()
        {
            Field("name", x => x.Name);
            Field("type", x => x.Type, type: typeof(AttributeValueTypeType));
            Field("isArray", x => x.IsArray, type: typeof(BooleanGraphType));
            Field("isID", x => x.IsID, type: typeof(BooleanGraphType));
            Field("valueConstraints", x => x.ValueConstraints, type: typeof(ListGraphType<AttributeValueConstraintType>));
        }
    }

    public class TraitRelationInputType : InputObjectGraphType<TraitRelation>
    {
        public TraitRelationInputType()
        {
            Field("identifier", x => x.Identifier);
            Field("template", x => x.RelationTemplate, type: typeof(RelationTemplateInputType));
        }
    }

    public class RelationTemplateInputType : InputObjectGraphType<RelationTemplate>
    {
        public RelationTemplateInputType()
        {
            Field("predicateID", x => x.PredicateID);
            Field("directionForward", x => x.DirectionForward);
            Field("traitHints", x => x.TraitHints, nullable: true);
        }
    }


    public class UpsertGeneratorInput
    {
        public string ID { get; private set; }
        public string AttributeName { get; private set; }
        public string AttributeValueTemplate { get; private set; }
    }
    public class UpsertGeneratorInputType : InputObjectGraphType<UpsertGeneratorInput>
    {
        public UpsertGeneratorInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.AttributeName);
            Field(x => x.AttributeValueTemplate);
        }
    }

    public class UpsertAuthRoleInput
    {
        public string ID { get; private set; }
        public string[] Permissions { get; private set; }
    }
    public class UpsertAuthRoleInputType : InputObjectGraphType<UpsertAuthRoleInput>
    {
        public UpsertAuthRoleInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.Permissions);
        }
    }

    public class UpsertValidatorContextInput
    {
        public string ID { get; private set; }
        public string ValidatorReference { get; private set; }
        public string Config { get; private set; }
    }
    public class UpsertValidatorContextInputType : InputObjectGraphType<UpsertValidatorContextInput>
    {
        public UpsertValidatorContextInputType()
        {
            Field("id", x => x.ID);
            Field("validatorReference", x => x.ValidatorReference);
            Field("config", x => x.Config);
        }
    }

    public class UpsertCLConfigInput
    {
        public string ID { get; private set; }
        public string CLBrainReference { get; private set; }
        public string CLBrainConfig { get; private set; }
    }
    public class UpsertCLConfigInputType : InputObjectGraphType<UpsertCLConfigInput>
    {
        public UpsertCLConfigInputType()
        {
            Field("id", x => x.ID);
            Field("clBrainReference", x => x.CLBrainReference);
            Field("clBrainConfig", x => x.CLBrainConfig);
        }
    }

    public class UpsertODataAPIContextInput
    {
        public string ID { get; private set; }
        public string Config { get; private set; }
    }
    public class UpsertODataAPIContextInputType : InputObjectGraphType<UpsertODataAPIContextInput>
    {
        public UpsertODataAPIContextInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.Config);
        }
    }
}
