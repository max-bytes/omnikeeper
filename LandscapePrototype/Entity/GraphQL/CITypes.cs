using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class CIType : ObjectGraphType<CI>
    {
        public CIType(RelationModel relationModel, CIModel ciModel, LayerModel layerModel)
        {
            Field(x => x.Identity);
            Field(x => x.Attributes, type: typeof(ListGraphType<CIAttributeType>));
            Field<ListGraphType<RelatedCIType>>("related",
            arguments: new QueryArguments(new List<QueryArgument>
            {
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                {
                    Name = "layers"
                },
            }),
            resolve: (context) =>
            {
                var CIIdentity = context.Source.Identity;
                var layerStrings = context.GetArgument<string[]>("layers");
                var layers = layerModel.BuildLayerSet(layerStrings);
                var relations = relationModel.GetMergedRelations(CIIdentity, false, layers, RelationModel.IncludeRelationDirections.Forward);
                return relations.Select(r =>
                {
                    var CIIdentity = ciModel.GetIdentityFromCIID(r.ToCIID);
                    return RelatedCI.Build(r, ciModel.GetCI(CIIdentity, layers));
                });
            });
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
