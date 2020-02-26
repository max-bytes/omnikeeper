using GraphQL.Types;
using LandscapePrototype.Model;
using System.Collections.Generic;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, LayerModel layerModel)
        {
            FieldAsync<CIType>("ci",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "identity"
                    },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    },
                }),
                resolve: async context =>
                {
                    var ciIdentity = context.GetArgument<string>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var layers = await layerModel.BuildLayerSet(layerStrings, null);

                    var ci = await ciModel.GetCI(ciIdentity, layers, null);

                    return ci;
                });

            FieldAsync<ListGraphType<CIType>>("cis",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    },
                }),
                resolve: async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var layers = await layerModel.BuildLayerSet(layerStrings, null);

                    var cis = await ciModel.GetCIs(layers, false, null);
                    return cis;
                });


            FieldAsync<ListGraphType<LayerType>>("layers",
                resolve: async context =>
                {
                    var layers = await layerModel.GetLayers(null);

                    return layers;
                });
        }
    }
}
