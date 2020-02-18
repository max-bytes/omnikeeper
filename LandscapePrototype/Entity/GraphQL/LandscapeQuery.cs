using GraphQL.Types;
using LandscapePrototype.Model;
using System.Collections.Generic;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, LayerModel layerModel)
        {
            FieldAsync<ListGraphType<CIType>>("ci",
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

                    return new List<CI>() { ci };
                });
        }
    }
}
