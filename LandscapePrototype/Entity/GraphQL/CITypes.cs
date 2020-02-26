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
            Field("id", x => x.ID);
            Field("layerhash", x => x.Layers.LayerHash);
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
                var relations = await relationModel.GetMergedRelations(CIIdentity, false, layers, RelationModel.IncludeRelationDirections.Forward, null);

                var relatedCIs = new List<RelatedCI>();
                
                foreach(var r in relations)
                {
                    var realtedCIIdentity = await ciModel.GetIdentityFromCIID(r.ToCIID, null);
                    relatedCIs.Add(RelatedCI.Build(r, await ciModel.GetCI(realtedCIIdentity, layers, null)));
                }

                var wStr = context.GetArgument<string>("where"); // TODO: develop further
                if (wStr != null)
                    try
                    {
                        relatedCIs = relatedCIs.AsQueryable().Where(wStr).ToList();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                return relatedCIs;
            });
        }
    }

    public class CIAttributeType : ObjectGraphType<CIAttribute>
    {
        public CIAttributeType(LayerModel layerModel)
        {
            Field(x => x.ActivationTime);
            Field("ciid", x => x.CIID);
            Field(x => x.LayerID);
            Field(x => x.LayerStackIDs);
            Field(x => x.Name);
            Field(x => x.State, type: typeof(AttributeStateType));
            Field(x => x.Value, type: typeof(AttributeValueType));

            FieldAsync<LayerType>("layer",
            resolve: async (context) =>
            {
                var layerID = context.Source.LayerID;
                var layer = await layerModel.GetLayer(layerID, null);
                return layer;
            });

            FieldAsync<ListGraphType<LayerType>>("layerstack",
            resolve: async (context) =>
            {
                // TODO: performance improvements
                var layerstackIDs = context.Source.LayerStackIDs;
                var layers = new List<Layer>();
                foreach(var id in layerstackIDs)
                    layers.Add(await layerModel.GetLayer(id, null));
                return layers;
            });
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