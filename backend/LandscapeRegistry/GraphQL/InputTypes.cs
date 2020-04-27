using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using System;

namespace LandscapeRegistry.GraphQL
{
    public class CreateCIInput
    {
        public string TypeID { get; private set; }
    }
    public class CreateCIInputType : InputObjectGraphType<CreateCIInput>
    {
        public CreateCIInputType()
        {
            Field(x => x.TypeID, nullable: true);
        }
    }

    public class CreateLayerInput
    {
        public string Name { get; private set; }
        public AnchorState State { get; private set; }
        public string BrainName { get; private set; }
    }
    public class CreateLayerInputType : InputObjectGraphType<CreateLayerInput>
    {
        public CreateLayerInputType()
        {
            Field(x => x.Name);
            Field(x => x.State, type: typeof(AnchorStateType));
            Field(x => x.BrainName, nullable: true);
        }
    }
    public class UpdateLayerInput
    {
        public long ID { get; private set; }
        public AnchorState State { get; private set; }
        public string BrainName { get; private set; }
    }
    public class UpdateLayerInputType : InputObjectGraphType<UpdateLayerInput>
    {
        public UpdateLayerInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.State, type: typeof(AnchorStateType));
            Field(x => x.BrainName);
        }
    }

    public class InsertCIAttributeInput
    {
        public Guid CI { get; private set; }
        public string Name { get; private set; }
        public long LayerID { get; private set; }
        public AttributeValueDTO Value { get; private set; }
    }
    public class InsertCIAttributeInputType : InputObjectGraphType<InsertCIAttributeInput>
    {
        public InsertCIAttributeInputType()
        {
            Field("ci", x => x.CI, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.Name);
            Field(x => x.LayerID);
            Field(x => x.Value, type: typeof(AttributeValueDTOInputType));
        }
    }

    public class RemoveCIAttributeInput
    {
        public Guid CI { get; private set; }
        public string Name { get; private set; }
        public long LayerID { get; private set; }
    }
    public class RemoveCIAttributeInputType : InputObjectGraphType<RemoveCIAttributeInput>
    {
        public RemoveCIAttributeInputType()
        {
            Field("ci", x => x.CI, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.Name);
            Field(x => x.LayerID);
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
        public long LayerID { get; private set; }
    }

    public class InsertRelationInputType : InputObjectGraphType<InsertRelationInput>
    {
        public InsertRelationInputType()
        {
            Field(x => x.FromCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.ToCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.PredicateID);
            Field(x => x.LayerID);
        }
    }

    public class RemoveRelationInput
    {
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public long LayerID { get; private set; }
    }
    public class RemoveRelationInputType : InputObjectGraphType<RemoveRelationInput>
    {
        public RemoveRelationInputType()
        {
            Field(x => x.FromCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.ToCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.PredicateID);
            Field(x => x.LayerID);
        }
    }

    public class UpsertPredicateInput
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }
        public AnchorState State { get; private set; }
    }
    public class UpsertPredicateInputType : InputObjectGraphType<UpsertPredicateInput>
    {
        public UpsertPredicateInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
            Field(x => x.State, type: typeof(AnchorStateType));
        }
    }

    public class UpsertCITypeInput
    {
        public string ID { get; private set; }
        public AnchorState State { get; private set; }
    }
    public class UpsertCITypeInputType : InputObjectGraphType<UpsertCITypeInput>
    {
        public UpsertCITypeInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.State, type: typeof(AnchorStateType));
        }
    }
}
