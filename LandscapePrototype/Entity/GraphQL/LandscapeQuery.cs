using GraphQL.Types;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, LayerModel layerModel, ChangesetModel changesetModel)
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
                    new QueryArgument<DateTimeOffsetGraphType>
                    {
                        Name = "timeThreshold"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;

                    var ciIdentity = context.GetArgument<string>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    try
                    {
                        userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);
                    }catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    var ci = await ciModel.GetCI(ciIdentity, userContext.LayerSet, null, userContext.TimeThreshold);

                    return ci;
                });

            FieldAsync<ListGraphType<CIType>>("cis",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    },
                    new QueryArgument<DateTimeOffsetGraphType>
                    {
                        Name = "timeThreshold"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;

                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    var cis = await ciModel.GetCIs(userContext.LayerSet, false, null, userContext.TimeThreshold);
                    return cis;
                });


            FieldAsync<ListGraphType<LayerType>>("layers",
                resolve: async context =>
                {
                    var layers = await layerModel.GetLayers(null);

                    return layers;
                });

            FieldAsync<ChangesetType>("changeset",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<LongGraphType>>
                    {
                        Name = "id"
                    }
                }),
                resolve: async context =>
                {
                    var id = context.GetArgument<long>("id");
                    var changeset = await changesetModel.GetChangeset(id, null);
                    return changeset;
                });

            FieldAsync<ListGraphType<ChangesetType>>("changesets",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>>
                    {
                        Name = "from"
                    },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>>
                    {
                        Name = "to"
                    },
                    new QueryArgument<LongGraphType>
                    {
                        Name = "ciid"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciid = context.GetArgument<long?>("ciid");
                    return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, ciid, null);
                });
        }
    }
}
