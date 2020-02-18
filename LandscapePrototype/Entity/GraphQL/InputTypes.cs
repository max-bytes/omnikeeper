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
    }
    public class CreateCIInputType : InputObjectGraphType<CreateCIInput>
    {
        public CreateCIInputType()
        {
            Field(x => x.Identity);
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
        public string Layer { get; private set; }
        public AttributeValueGeneric Value { get; private set; }
    }

    public class InsertCIAttributeInputType : InputObjectGraphType<InsertCIAttributeInput>
    {
        public InsertCIAttributeInputType()
        {
            Field(x => x.CI);
            Field(x => x.Name);
            Field(x => x.Layer);
            Field(x => x.Value, type: typeof(AttributeValueGenericType));
        }
    }

    public class AttributeValueGenericType : InputObjectGraphType<AttributeValueGeneric>
    {
        public AttributeValueGenericType()
        {
            Field(x => x.Type);
            Field(x => x.Value);
        }
    }
}
