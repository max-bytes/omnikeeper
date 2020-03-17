using GraphQL.Types;
using Landscape.Base;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace LandscapePrototype.Entity.GraphQL
{
    public class LandscapeQuery : ObjectGraphType
    {
        public LandscapeQuery(CIModel ciModel, LayerModel layerModel, PredicateModel predicateModel, ChangesetModel changesetModel)
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
                    var ls = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    var ci = await ciModel.GetFullCI(ciIdentity, userContext.LayerSet, null, userContext.TimeThreshold);

                    return ci;
                });

            FieldAsync<ListGraphType<CIType>>("cis",
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

                    var cis = await ciModel.GetFullCIs(userContext.LayerSet, includeEmpty, null, userContext.TimeThreshold);
                    return cis;
                });
            FieldAsync<ListGraphType<PredicateType>>("predicates",
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", DateTimeOffset.Now);

                    return (await predicateModel.GetPredicates(null, userContext.TimeThreshold)).Values;
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
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as LandscapeUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciid = context.GetArgument<string>("ciid");
                    return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, RelationModel.IncludeRelationDirections.Both, ciid, null);
                });
        }
    }
}
