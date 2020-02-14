using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class CIType : ObjectGraphType<CI>
    {
        public CIType()
        {
            Field(x => x.Attributes, type: typeof(ListGraphType<CIAttributeType>));
        }
    }

    public class CIAttributeType : ObjectGraphType<CIAttribute>
    {
        public CIAttributeType()
        {
            Field(x => x.ActivationTime);
            Field(x => x.CIID);
            Field(x => x.LayerID);
            Field(x => x.Name);
            Field(x => x.State, type: typeof(AttributeStateType));
            Field(x => x.Value, type: typeof(AttributeValueType));
        }
    }

    public class AttributeStateType : EnumerationGraphType<AttributeState>
    {
    }

    public class AttributeValueType : UnionGraphType
    {
        public AttributeValueType()
        {
            Type<AttributeValueIntegerType>();
            Type<AttributeValueTextType>();
        }
    }

    public class AttributeValueIntegerType : ObjectGraphType<AttributeValueInteger>
    {
        public AttributeValueIntegerType()
        {
            Field(x => x.Value);
        }
    }
    public class AttributeValueTextType : ObjectGraphType<AttributeValueText>
    {
        public AttributeValueTextType()
        {
            Field(x => x.Value);
        }
    }
}
