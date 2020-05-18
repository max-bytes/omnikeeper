using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.GraphQL
{
    public class RegistryQuery : ObjectGraphType
    {
        public RegistryQuery(ICIModel ciModel, ILayerModel layerModel, IPredicateModel predicateModel, 
            IChangesetModel changesetModel, ICISearchModel ciSearchModel, ITraitsProvider traitsProvider)
        {
            FieldAsync<MergedCIType>("ci",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<GuidGraphType>>
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
                    var userContext = context.UserContext as RegistryUserContext;

                    var ciid = context.GetArgument<Guid>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    var ci = await ciModel.GetMergedCI(ciid, userContext.LayerSet, null, userContext.TimeThreshold);

                    return ci;
                });

            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var ciids = await ciModel.GetCIIDs(null);
                    return ciids;
                });

            FieldAsync<ListGraphType<CompactCIType>>("compactCIs",
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
                    var userContext = context.UserContext as RegistryUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    var cis = await ciModel.GetCompactCIs(userContext.LayerSet, null, userContext.TimeThreshold);
                    return cis;
                });

            FieldAsync<ListGraphType<CompactCIType>>("searchCIs",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "searchString"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;

                    var ciid = context.GetArgument<Guid>("identity");
                    //var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(null);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    return await ciSearchModel.Search(context.GetArgument<string>("searchString"), ls, null, userContext.TimeThreshold);
                });

            //FieldAsync<ListGraphType<MergedCIType>>("cis",
            //    arguments: new QueryArguments(new List<QueryArgument>
            //    {
            //        new QueryArgument<ListGraphType<StringGraphType>>
            //        {
            //            Name = "layers"
            //        },
            //        new QueryArgument<DateTimeOffsetGraphType>
            //        {
            //            Name = "timeThreshold"
            //        },
            //        new QueryArgument<BooleanGraphType>
            //        {
            //            Name = "includeEmpty"
            //        },
            //    }),
            //    resolve: async context =>
            //    {
            //        var userContext = context.UserContext as RegistryUserContext;

            //        var layerStrings = context.GetArgument<string[]>("layers");
            //        var layerSet = layerStrings != null ? await layerModel.BuildLayerSet(layerStrings, null) : await layerModel.BuildLayerSet(null);
            //        userContext.LayerSet = layerSet;
            //        var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
            //        userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

            //        var includeEmpty = context.GetArgument("includeEmpty", false);

            //        var cis = await ciModel.GetMergedCIs(userContext.LayerSet, includeEmpty, null, userContext.TimeThreshold);
            //        return cis;
            //    });
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
                    var userContext = context.UserContext as RegistryUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");

                    return (await predicateModel.GetPredicates(null, userContext.TimeThreshold, stateFilter)).Values;
                });
            FieldAsync<ListGraphType<CITypeType>>("citypes",
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());

                    return await ciModel.GetCITypes(null, userContext.TimeThreshold);
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
                    new QueryArgument<GuidGraphType>
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
                    var userContext = context.UserContext as RegistryUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciid = context.GetArgument<Guid?>("ciid", null);
                    var limit = context.GetArgument<int?>("limit", null);
                    if (ciid != null)
                        return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, IncludeRelationDirections.Both, ciid.Value, null, limit);
                    else
                        return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, IncludeRelationDirections.Both, null, limit);
                });


            FieldAsync<StringGraphType>("traits",
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();// context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    // TODO: implement properly, just showing json string for now
                    var traitsJSON = JObject.FromObject(traitsProvider.GetTraits());
                    return traitsJSON.ToString();
                });

        }
    }
}
