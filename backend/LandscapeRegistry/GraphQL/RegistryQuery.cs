using GraphQL.Types;
using Keycloak.Net.Models.Root;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Landscape.Base.Model.IRelationModel;

namespace LandscapeRegistry.GraphQL
{
    public class RegistryQuery : ObjectGraphType
    {
        public RegistryQuery(ICIModel ciModel, ILayerModel layerModel, IPredicateModel predicateModel, IMemoryCacheModel memoryCacheModel,
            IChangesetModel changesetModel, ICISearchModel ciSearchModel, ITraitModel traitModel, ITraitsProvider traitsProvider)
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
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "withEffectiveTraits"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;

                    var searchString = context.GetArgument<string>("searchString");
                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits");
                    var ciid = context.GetArgument<Guid>("identity");
                    var ls = await layerModel.BuildLayerSet(null);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    return await ciSearchModel.Search(searchString, withEffectiveTraits, ls, null, userContext.TimeThreshold);
                });

            FieldAsync<ListGraphType<CompactCIType>>("validRelationTargetCIs",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    },
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "predicateID"
                    },
                    new QueryArgument<NonNullGraphType<BooleanGraphType>>
                    {
                        Name = "forward"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    var predicateID = context.GetArgument<string>("predicateID");
                    var forward = context.GetArgument<bool>("forward");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var predicates = (await predicateModel.GetPredicates(null, userContext.TimeThreshold, AnchorStateFilter.ActiveOnly));
                    var predicate = predicates[predicateID];

                    // predicate has no target constraints -> makes it easy, return ALL CIs
                    if ((forward && !predicate.Constraints.HasPreferredTraitsTo) || (!forward && !predicate.Constraints.HasPreferredTraitsFrom))
                        return await ciModel.GetCompactCIs(userContext.LayerSet, null, userContext.TimeThreshold);

                    var preferredTraits = (forward) ? predicate.Constraints.PreferredTraitsTo : predicate.Constraints.PreferredTraitsFrom;

                    // TODO: this has abysmal performance! We fully query ALL CIs and the calculate the effective traits for each of them... :(
                    var allCIIDs = await ciModel.GetCIIDs(null);
                    var cis = await ciModel.GetMergedCIs(userContext.LayerSet, true, null, userContext.TimeThreshold, allCIIDs);
                    var effectiveTraitSets = await traitModel.CalculateEffectiveTraitSetForCIs(cis, preferredTraits, null, userContext.TimeThreshold);

                    return effectiveTraitSets.Where(et =>
                    { 
                        // if CI has ANY of the preferred traits, keep it
                        return preferredTraits.Any(pt => et.EffectiveTraits.ContainsKey(pt));
                    }).Select(et => CompactCI.Build(et.UnderlyingCI));
                });

            FieldAsync<ListGraphType<DirectedPredicateType>>("directedPredicates",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<GuidGraphType>>
                    {
                        Name = "preferredForCI"
                    },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layersForEffectiveTraits"
                    },
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");
                    var preferredForCI = context.GetArgument<Guid>("preferredForCI");
                    var layersForEffectiveTraits = context.GetArgument<string[]>("layersForEffectiveTraits");

                    var predicates = (await predicateModel.GetPredicates(null, userContext.TimeThreshold, AnchorStateFilter.ActiveOnly)).Values;

                    // filter predicates by constraints
                    var layers = await layerModel.BuildLayerSet(layersForEffectiveTraits, null);
                    var ci = await ciModel.GetMergedCI(preferredForCI, layers, null, userContext.TimeThreshold);
                    var effectiveTraitSet = await traitModel.CalculateEffectiveTraitSetForCI(ci, null, userContext.TimeThreshold);
                    var effectiveTraitNames = effectiveTraitSet.EffectiveTraits.Keys;
                    var directedPredicates = predicates.SelectMany(predicate =>
                    {
                        var ret = new List<DirectedPredicate>();
                        if (!predicate.Constraints.HasPreferredTraitsFrom || predicate.Constraints.PreferredTraitsFrom.Any(pt => effectiveTraitNames.Contains(pt)))
                            ret.Add(DirectedPredicate.Build(predicate.ID, predicate.WordingFrom, predicate.State, true));
                        if (!predicate.Constraints.HasPreferredTraitsTo || predicate.Constraints.PreferredTraitsTo.Any(pt => effectiveTraitNames.Contains(pt)))
                            ret.Add(DirectedPredicate.Build(predicate.ID, predicate.WordingTo, predicate.State, false)); // TODO: switch wording
                        return ret;
                    });

                    return directedPredicates;
                });

            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<AnchorStateFilterType>>
                    {
                        Name = "stateFilter"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");
                    
                    var predicates = (await predicateModel.GetPredicates(null, userContext.TimeThreshold, stateFilter)).Values;

                    return predicates;
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
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

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
                    // TODO: implement properly, just showing json string for now
                    var traitsJSON = JObject.FromObject(traitsProvider.GetTraits());
                    return traitsJSON.ToString();
                });

            FieldAsync<ListGraphType<EffectiveTraitListItemType>>("effectiveTraitList",
                arguments: new QueryArguments(new List<QueryArgument>
                {
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>>
                    {
                        Name = "layers"
                    }
                }),
                resolve: async context =>
                {
                    var userContext = context.UserContext as RegistryUserContext;
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, null);
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    // TODO: HORRIBLE performance!, consider aggressive caching
                    var traits = traitsProvider.GetTraits();
                    var ret = new List<(string name, int count) > ();
                    foreach(var trait in traits.Values)
                    {
                        var ets = await traitModel.CalculateEffectiveTraitSetsForTrait(trait, userContext.LayerSet, null, userContext.TimeThreshold);
                        ret.Add((name: trait.Name, count: ets.Count()));
                    }
                    return ret;
                });

            Field<ListGraphType<StringGraphType>>("cacheKeys",
                resolve: context =>
                {
                    var keys = memoryCacheModel.GetKeys();
                    return keys;
                });
        }
    }
}
