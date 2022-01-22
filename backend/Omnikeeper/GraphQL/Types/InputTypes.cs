#nullable disable // TODO

using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using System;

namespace Omnikeeper.GraphQL.Types
{
    public class CreateCIInput
    {
        public string Name { get; private set; }
        public string LayerIDForName { get; private set; }
    }
    public class CreateCIInputType : InputObjectGraphType<CreateCIInput>
    {
        public CreateCIInputType()
        {
            Field(x => x.Name);
            Field(x => x.LayerIDForName);
        }
    }

    public class UpsertLayerDataInput
    {
        public string ID { get; private set; }
        public string Description { get; private set; }
        public AnchorState State { get; private set; }
        public string CLConfigID { get; private set; }
        public string OnlineInboundAdapterName { get; private set; }
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
            Field(x => x.OnlineInboundAdapterName, nullable: true);
            Field(x => x.Color);
            Field(x => x.Generators);
        }
    }

    public class InsertCIAttributeInput
    {
        public Guid CI { get; private set; }
        public string Name { get; private set; }
        public AttributeValueDTO Value { get; private set; }
    }
    public class InsertCIAttributeInputType : InputObjectGraphType<InsertCIAttributeInput>
    {
        public InsertCIAttributeInputType()
        {
            Field("ci", x => x.CI, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.Name);
            Field(x => x.Value, type: typeof(AttributeValueDTOInputType));
        }
    }

    public class RemoveCIAttributeInput
    {
        public Guid CI { get; private set; }
        public string Name { get; private set; }
    }
    public class RemoveCIAttributeInputType : InputObjectGraphType<RemoveCIAttributeInput>
    {
        public RemoveCIAttributeInputType()
        {
            Field("ci", x => x.CI, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.Name);
        }
    }

    public class AttributeValueDTOInputType : InputObjectGraphType<AttributeValueDTO>
    {
        public AttributeValueDTOInputType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field(x => x.Values);
            Field(x => x.IsArray);
        }
    }

    public class InsertRelationInput
    {
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
    }

    public class InsertRelationInputType : InputObjectGraphType<InsertRelationInput>
    {
        public InsertRelationInputType()
        {
            Field(x => x.FromCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.ToCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.PredicateID);
        }
    }

    public class RemoveRelationInput
    {
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
    }
    public class RemoveRelationInputType : InputObjectGraphType<RemoveRelationInput>
    {
        public RemoveRelationInputType()
        {
            Field(x => x.FromCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.ToCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.PredicateID);
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
        public string[] RequiredAttributes { get; private set; }
        public string[] OptionalAttributes { get; private set; }
        public string[] OptionalRelations { get; private set; }
        public string[] RequiredTraits { get; private set; }
    }
    public class UpsertRecursiveTraitInputType : InputObjectGraphType<UpsertRecursiveTraitInput>
    {
        public UpsertRecursiveTraitInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.RequiredAttributes);
            Field(x => x.OptionalAttributes);
            Field<StringGraphType>(name: "requiredRelations", resolve: x => "", deprecationReason: "not used anymore"); // TODO: remove
            Field(x => x.OptionalRelations);
            Field(x => x.RequiredTraits);
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

    public class CreateOIAContextInput
    {
        public string Name { get; private set; }
        public string Config { get; private set; }
    }
    public class CreateOIAContextInputType : InputObjectGraphType<CreateOIAContextInput>
    {
        public CreateOIAContextInputType()
        {
            Field(x => x.Name);
            Field(x => x.Config);
        }
    }
    public class UpdateOIAContextInput
    {
        public long ID { get; private set; }
        public string Name { get; private set; }
        public string Config { get; private set; }
    }
    public class UpdateOIAContextInputType : InputObjectGraphType<UpdateOIAContextInput>
    {
        public UpdateOIAContextInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.Name);
            Field(x => x.Config);
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

    //public class CIAttributeTemplateInputType : InputObjectGraphType<CIAttributeTemplate>
    //{
    //    public CIAttributeTemplateInputType()
    //    {
    //        Field(x => x.Name);
    //        Field(x => x.IsArray);
    //        Field(x => x.Type, type: typeof(AttributeValueTypeType));
    //        Field("valueConstraints", x => x.ValueConstraints, type: typeof(ListGraphType<CIAttributeValueConstraintType>));

    //    }
    //}

    //public class CIAttributeValueConstraintType : InputObjectGraphType<ICIAttributeValueConstraint>
    //{

    //}
}
