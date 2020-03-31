using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
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
        public AttributeValueGenericScalar Value { get; private set; }
    }
    public class InsertCIAttributeInputType : InputObjectGraphType<InsertCIAttributeInput>
    {
        public InsertCIAttributeInputType()
        {
            Field("ci", x => x.CI);
            Field(x => x.Name);
            Field(x => x.LayerID);
            Field(x => x.Value, type: typeof(AttributeValueGenericInputType));
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

    public class AttributeValueGenericInputType : InputObjectGraphType<AttributeValueGenericScalar>
    {
        public AttributeValueGenericInputType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field(x => x.Value);
        }
    }

    public class InsertRelationInput { 
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
}
