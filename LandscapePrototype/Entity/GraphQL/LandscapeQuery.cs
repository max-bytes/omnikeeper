using GraphQL.Types;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, LayerModel layerModel)
        {
            Field<ListGraphType<CIType>>("ci",
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
                resolve: context =>
                {
                    var ciIdentity = context.GetArgument<string>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers");

                    var layers = layerModel.BuildLayerSet(layerStrings);

                    var ci = ciModel.GetCI(ciIdentity, layers);

                    return new List<CI>() { ci };
            });
        }
    }
}
