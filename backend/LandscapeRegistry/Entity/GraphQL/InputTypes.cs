using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using LandscapeRegistry.Entity.AttributeValues;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class CreateCIInput
    {
        public string Identity { get; private set; }
        public string TypeID { get; private set; }
    }
    public class CreateCIInputType : InputObjectGraphType<CreateCIInput>
    {
        public CreateCIInputType()
        {
            Field(x => x.Identity);
            Field(x => x.TypeID);
        }
    }

    public class CreateLayerInput
    {
        public string Name { get; private set; }
    }
    public class CreateLayerInputType : InputObjectGraphType<CreateLayerInput>
    {
        public CreateLayerInputType()
        {
            Field(x => x.Name);
        }
    }

    public class InsertCIAttributeInput
    {
        public string CI { get; private set; }
        public string Name { get; private set; }
        public long LayerID { get; private set; }
        public AttributeValueDTO Value { get; private set; }
    }
    public class InsertCIAttributeInputType : InputObjectGraphType<InsertCIAttributeInput>
    {
        public InsertCIAttributeInputType()
        {
            Field("ci", x => x.CI);
            Field(x => x.Name);
            Field(x => x.LayerID);
            Field(x => x.Value, type: typeof(AttributeValueDTOInputType));
        }
    }

    public class RemoveCIAttributeInput
    {
        public string CI { get; private set; }
        public string Name { get; private set; }
        public long LayerID { get; private set; }
    }
    public class RemoveCIAttributeInputType : InputObjectGraphType<RemoveCIAttributeInput>
    {
        public RemoveCIAttributeInputType()
        {
            Field("ci", x => x.CI);
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
        public string FromCIID { get; private set; }
        public string ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public long LayerID { get; private set; }
    }

    public class InsertRelationInputType : InputObjectGraphType<InsertRelationInput>
    {
        public InsertRelationInputType()
        {
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.LayerID);
        }
    }

    public class RemoveRelationInput
    {
        public string FromCIID { get; private set; }
        public string ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public long LayerID { get; private set; }
    }
    public class RemoveRelationInputType : InputObjectGraphType<RemoveRelationInput>
    {
        public RemoveRelationInputType()
        {
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.LayerID);
        }
    }

    public class MutatePredicateInput
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }
        public PredicateState State { get; private set; }
    }
    public class MutatePredicateInputType : InputObjectGraphType<MutatePredicateInput>
    {
        public MutatePredicateInputType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
            Field(x => x.State, type: typeof(PredicateStateType));
        }
    }
}
