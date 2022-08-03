#nullable disable // TODO

using GraphQL.Types;
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
        public bool Mask { get; private set; }
    }

    public class InsertRelationInputType : InputObjectGraphType<InsertRelationInput>
    {
        public InsertRelationInputType()
        {
            Field(x => x.FromCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.ToCIID, type: typeof(NonNullGraphType<GuidGraphType>));
            Field(x => x.PredicateID);
            Field(x => x.Mask);
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

    public class InsertChangesetDataAttributeInput
    {
        public string Name { get; private set; }
        public AttributeValueDTO Value { get; private set; }
    }
    public class InsertChangesetDataAttributeInputType : InputObjectGraphType<InsertChangesetDataAttributeInput>
    {
        public InsertChangesetDataAttributeInputType()
        {
            Field(x => x.Name);
            Field(x => x.Value, type: typeof(AttributeValueDTOInputType));
        }
    }
}
