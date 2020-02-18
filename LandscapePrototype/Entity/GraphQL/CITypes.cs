using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace LandscapePrototype.Entity.GraphQL
{
    public class CIType : ObjectGraphType<CI>
    {
        public CIType(RelationModel relationModel, CIModel ciModel, LayerModel layerModel)
        {
            Field(x => x.Identity);
            Field(x => x.Attributes, type: typeof(ListGraphType<CIAttributeType>));
            FieldAsync<ListGraphType<RelatedCIType>>("related",
            arguments: new QueryArguments(new List<QueryArgument>
            {
                new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                new QueryArgument<StringGraphType> { Name = "where" },
            }),
            resolve: async (context) =>
            {
                var CIIdentity = context.Source.Identity;
                var layerStrings = context.GetArgument<string[]>("layers");
                var layers = await layerModel.BuildLayerSet(layerStrings, null);
                var relations = await relationModel.GetMergedRelations(CIIdentity, false, layers, RelationModel.IncludeRelationDirections.Forward);
                var r = await Task.WhenAll(relations.Select(async r =>
                {
                    var CIIdentity = await ciModel.GetIdentityFromCIID(r.ToCIID, null);
                    return RelatedCI.Build(r, await ciModel.GetCI(CIIdentity, layers, null));
                }));

                var wStr = context.GetArgument<string>("where"); // TODO: develop further
                if (wStr != null)
                    try
                    {
                        r = r.AsQueryable().Where(wStr).ToArray();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                return r;
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