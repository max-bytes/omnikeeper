using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using System;
using System.Collections.Generic;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, CachedLayerModel layerModel, CachedPredicateModel predicateModel, ChangesetModel changesetModel)
        {
            FieldAsync<MergedCIType>("ci",
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
                    var ls = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    var ci = await ciModel.GetMergedCI(ciIdentity, userContext.LayerSet, null, userContext.TimeThreshold);

                    return ci;
                });

            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var ciids = await ciModel.GetCIIDs(null);
                    return ciids;
                });

            FieldAsync<ListGraphType<MergedCIType>>("cis",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<ListGraphType<StringGraphType>>
                    {
                        Name = "layers"
                    },
                    new QueryArgument<DateTimeOffsetGraphType>
                    {
                        Name = "timeThreshold"
                    },
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeEmpty"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;

                    var layerStrings = context.GetArgument<string[]>("layers");
                    var layerSet = (layerStrings != null) ? await layerModel.BuildLayerSet(layerStrings, null) : await layerModel.BuildLayerSet(null);
                    userContext.LayerSet = layerSet;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    var includeEmpty = context.GetArgument<bool>("includeEmpty", false);

                    var cis = await ciModel.GetMergedCIs(userContext.LayerSet, includeEmpty, null, userContext.TimeThreshold);
                    return cis;
                });
            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<AnchorStateFilterType>>
                    {
                        Name = "stateFilter"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");

                    return (await predicateModel.GetPredicates(null, userContext.TimeThreshold, stateFilter)).Values;
                });
            FieldAsync<ListGraphType<CITypeType>>("citypes",
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    return (await ciModel.GetCITypes(null));
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
                    new QueryArgument<StringGraphType>
                    {
                        Name = "ciid"
                    },
                    new QueryArgument<IntGraphType>
                    {
                        Name = "limit"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciid = context.GetArgument<string>("ciid", null);
                    var limit = context.GetArgument<int?>("limit", null);
                    if (ciid != null)
                        return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, IncludeRelationDirections.Both, ciid, null, limit);
                    else
                        return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, IncludeRelationDirections.Both, null, limit);
                });
        }
    }
}
