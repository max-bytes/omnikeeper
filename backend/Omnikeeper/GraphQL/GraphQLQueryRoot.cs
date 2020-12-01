using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static Omnikeeper.Base.Model.IChangesetModel;
namespace Omnikeeper.GraphQL
{
    public class GraphQLQueryRoot : ObjectGraphType
    {
        public GraphQLQueryRoot()
        {
            FieldAsync<MergedCIType>("ci",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var ciid = context.GetArgument<Guid>("ciid");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    if (!ciBasedAuthorizationService.CanReadCI(ciid))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {ciid}");

                    var ci = await ciModel.GetMergedCI(ciid, userContext.LayerSet, userContext.Transaction, userContext.TimeThreshold);

                    return ci;
                });
            FieldAsync<ListGraphType<MergedCIType>>("cis",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    ICIIDSelection ciidSelection = new AllCIIDsSelection();  // if null, query all CIs
                    if (ciids != null)
                    {
                        if (!ciBasedAuthorizationService.CanReadAllCIs(ciids, out var notAllowedCI))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {notAllowedCI}");
                        ciidSelection = SpecificCIIDsSelection.Build(ciids);
                    }

                    var cis = await ciModel.GetMergedCIs(ciidSelection, userContext.LayerSet, false, userContext.Transaction, userContext.TimeThreshold);

                    if (ciidSelection is AllCIIDsSelection)
                    {
                        // reduce CIs to those that are allowed
                        cis = cis.Where(ci => ciBasedAuthorizationService.CanReadCI(ci.ID));
                    }

                    return cis;
                });

            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var ciids = await ciModel.GetCIIDs(userContext.Transaction);
                    // reduce CIs to those that are allowed
                    ciids = ciids.Where(ciid => ciBasedAuthorizationService.CanReadCI(ciid));
                    return ciids;
                });

            FieldAsync<ListGraphType<CompactCIType>>("compactCIs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    var cis = await ciModel.GetCompactCIs(new AllCIIDsSelection(), userContext.LayerSet, userContext.Transaction, userContext.TimeThreshold);
                    // reduce CIs to those that are allowed
                    cis = cis.Where(ci => ciBasedAuthorizationService.CanReadCI(ci.ID));
                    return cis;
                });

            FieldAsync<ListGraphType<CompactCIType>>("simpleSearchCIs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "searchString" }),
                resolve: async context =>
                {
                    var ciSearchModel = context.RequestServices.GetRequiredService<ICISearchModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var searchString = context.GetArgument<string>("searchString");
                    var ciid = context.GetArgument<Guid>("identity");
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var cis = await ciSearchModel.SimpleSearch(searchString, userContext.Transaction, userContext.TimeThreshold);
                    // reduce CIs to those that are allowed
                    cis = cis.Where(ci => ciBasedAuthorizationService.CanReadCI(ci.ID));
                    return cis;
                });

            FieldAsync<ListGraphType<CompactCIType>>("advancedSearchCIs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "searchString" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "withEffectiveTraits" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var ciSearchModel = context.RequestServices.GetRequiredService<ICISearchModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var searchString = context.GetArgument<string>("searchString");
                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits");
                    var ciid = context.GetArgument<Guid>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var cis = await ciSearchModel.AdvancedSearch(searchString, withEffectiveTraits, ls, userContext.Transaction, userContext.TimeThreshold);
                    // reduce CIs to those that are allowed
                    cis = cis.Where(ci => ciBasedAuthorizationService.CanReadCI(ci.ID));
                    return cis;
                });

            FieldAsync<ListGraphType<CompactCIType>>("validRelationTargetCIs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "predicateID" },
                    new QueryArgument<NonNullGraphType<BooleanGraphType>> { Name = "forward" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var effectiveTraitModel = context.RequestServices.GetRequiredService<IEffectiveTraitModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var predicateID = context.GetArgument<string>("predicateID");
                    var forward = context.GetArgument<bool>("forward");
                    var layerStrings = context.GetArgument<string[]>("layers");
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var predicate = await predicateModel.GetPredicate(predicateID, userContext.TimeThreshold, AnchorStateFilter.ActiveOnly, userContext.Transaction);
                    if (predicate == null)
                        throw new ExecutionError($"Could not find predicate with ID {predicateID}");

                    IEnumerable<CompactCI> cis;
                    // predicate has no target constraints -> makes it easy, return ALL CIs
                    if ((forward && !predicate.Constraints.HasPreferredTraitsTo) || (!forward && !predicate.Constraints.HasPreferredTraitsFrom))
                        cis = await ciModel.GetCompactCIs(new AllCIIDsSelection(), userContext.LayerSet, userContext.Transaction, userContext.TimeThreshold);
                    else
                    {
                        var preferredTraits = (forward) ? predicate.Constraints.PreferredTraitsTo : predicate.Constraints.PreferredTraitsFrom;

                        // TODO: this has abysmal performance! We fully query ALL CIs and then calculate the effective traits for each of them... :(
                        // we definitely have to look into caching traits as best as we can and provide a better way to query cis with a (array of) effective trait(s) as input
                        // we might alternatively need to rework this: limit the number of items this works on (with a limit parameter) and provide a search parameter
                        var allCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), userContext.LayerSet, true, userContext.Transaction, userContext.TimeThreshold);
                        var effectiveTraitSets = await effectiveTraitModel.CalculateEffectiveTraitSetForCIs(allCIs, preferredTraits, userContext.Transaction, userContext.TimeThreshold);

                        cis = effectiveTraitSets.Where(et =>
                        {
                            // if CI has ANY of the preferred traits, keep it
                            return preferredTraits.Any(pt => et.EffectiveTraits.ContainsKey(pt));
                        }).Select(et => CompactCI.BuildFromMergedCI(et.UnderlyingCI));
                    }

                    // reduce CIs to those that are allowed
                    cis = cis.Where(ci => ciBasedAuthorizationService.CanReadCI(ci.ID));

                    return cis;
                });

            FieldAsync<ListGraphType<DirectedPredicateType>>("directedPredicates",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "preferredForCI" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layersForEffectiveTraits" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                    var ciModel = context.RequestServices.GetRequiredService<ICIModel>();
                    var effectiveTraitModel = context.RequestServices.GetRequiredService<IEffectiveTraitModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");
                    var preferredForCI = context.GetArgument<Guid>("preferredForCI");
                    var layersForEffectiveTraits = context.GetArgument<string[]>("layersForEffectiveTraits");

                    if (!ciBasedAuthorizationService.CanReadCI(preferredForCI))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {preferredForCI}");

                    var predicates = (await predicateModel.GetPredicates(userContext.Transaction, userContext.TimeThreshold, AnchorStateFilter.ActiveOnly)).Values;

                    // filter predicates by constraints
                    var layers = await layerModel.BuildLayerSet(layersForEffectiveTraits, userContext.Transaction);
                    var ci = await ciModel.GetMergedCI(preferredForCI, layers, userContext.Transaction, userContext.TimeThreshold);
                    var effectiveTraitSet = await effectiveTraitModel.CalculateEffectiveTraitSetForCI(ci, userContext.Transaction, userContext.TimeThreshold);
                    var effectiveTraitNames = effectiveTraitSet.EffectiveTraits.Keys;
                    var directedPredicates = predicates.SelectMany(predicate =>
                    {
                        var ret = new List<DirectedPredicate>();
                        if (!predicate.Constraints.HasPreferredTraitsFrom || predicate.Constraints.PreferredTraitsFrom.Any(pt => effectiveTraitNames.Contains(pt)))
                            ret.Add(new DirectedPredicate(predicate.ID, predicate.State, predicate.WordingFrom, true));
                        if (!predicate.Constraints.HasPreferredTraitsTo || predicate.Constraints.PreferredTraitsTo.Any(pt => effectiveTraitNames.Contains(pt)))
                            ret.Add(new DirectedPredicate(predicate.ID, predicate.State, predicate.WordingTo, false)); // TODO: switch wording
                        return ret;
                    });

                    return directedPredicates;
                });

            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AnchorStateFilterType>> { Name = "stateFilter" }),
                resolve: async context =>
                {
                    var predicateModel = context.RequestServices.GetRequiredService<IPredicateModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());
                    var stateFilter = context.GetArgument<AnchorStateFilter>("stateFilter");

                    var predicates = (await predicateModel.GetPredicates(userContext.Transaction, userContext.TimeThreshold, stateFilter)).Values;

                    return predicates;
                });

            FieldAsync<ListGraphType<LayerType>>("layers",
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>(); 
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var layers = await layerModel.GetLayers(userContext.Transaction);

                    return layers;
                });

            FieldAsync<LayerStatisticsType>("layerStatistics",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<LongGraphType>> { Name = "layerID" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var layerStatisticsModel = context.RequestServices.GetRequiredService<ILayerStatisticsModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var layerID = context.GetArgument<long>("layerID");

                    var layer = await layerModel.GetLayer(layerID, userContext.Transaction);
                    if (layer == null)
                        throw new Exception($"Could not get layer with ID {layerID}");


                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributes(layer, userContext.Transaction);

                    var numAttributeChangesHistory = await layerStatisticsModel.GetAttributeChangesHistory(layer, userContext.Transaction);

                    var numActiveRelations = await layerStatisticsModel.GetActiveRelations(layer, userContext.Transaction);

                    var numRelationChangesHistory = await layerStatisticsModel.GetRelationChangesHistory(layer, userContext.Transaction);

                    var numLayerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layer, userContext.Transaction);

                    return new LayerStatistics(
                        layer,
                        numActiveAttributes,
                        numAttributeChangesHistory,
                        numActiveRelations,
                        numRelationChangesHistory,
                        numLayerChangesetsHistory);
                });

            FieldAsync<ListGraphType<OIAContextType>>("oiacontexts",
                resolve: async context =>
                {
                    var oiaContextModel = context.RequestServices.GetRequiredService<IOIAContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var configs = await oiaContextModel.GetContexts(true, userContext.Transaction);

                    return configs;
                });

            FieldAsync<ListGraphType<ODataAPIContextType>>("odataapicontexts",
                resolve: async context =>
                {
                    var odataAPIContextModel = context.RequestServices.GetRequiredService<IODataAPIContextModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var configs = await odataAPIContextModel.GetContexts(userContext.Transaction);

                    return configs;
                });

            FieldAsync<StringGraphType>("baseConfiguration",
                resolve: async context =>
                {
                    var baseConfigurationModel = context.RequestServices.GetRequiredService<IBaseConfigurationModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var cfg = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    return BaseConfigurationV1.Serializer.SerializeToString(cfg);
                });

            FieldAsync<ChangesetType>("changeset",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "id" }),
                resolve: async context =>
                {
                    var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var id = context.GetArgument<Guid>("id");
                    var changeset = await changesetModel.GetChangeset(id, userContext.Transaction);
                    return changeset;
                });

            FieldAsync<ListGraphType<ChangesetType>>("changesets",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>> { Name = "from" },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>> { Name = "to" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<IntGraphType> { Name = "limit" }),
                resolve: async context =>
                {
                    var changesetModel = context.RequestServices.GetRequiredService<IChangesetModel>();
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    var limit = context.GetArgument<int?>("limit", null);
                    IChangesetSelection selection = new ChangesetSelectionAllCIs();
                    if (ciids != null)
                        selection = ChangesetSelectionMultipleCIs.Build(ciids);

                    // NOTE: we can't filter the changesets using CIBasedAuthorizationService because changesets are not bound to CIs

                    return await changesetModel.GetChangesetsInTimespan(from, to, userContext.LayerSet, selection, userContext.Transaction, limit);
                });


            FieldAsync<StringGraphType>("traitSet",
                resolve: async context =>
                {
                    var traitModel = context.RequestServices.GetRequiredService<IRecursiveTraitModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    // TODO: implement better, showing string as-is for now
                    // TODO: should we not deliver non-DB traits (f.e. from CLBs) here?
                    var traitSet = await traitModel.GetRecursiveTraitSet(userContext.Transaction, TimeThreshold.BuildLatest());
                    var str = TraitsProvider.TraitSetSerializer.SerializeToString(traitSet);
                    return str;
                });

            // returns counts for each trait within the specified layers
            // TODO: consider renaming
            FieldAsync<ListGraphType<EffectiveTraitListItemType>>("effectiveTraitList",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
                {
                    var traitsProvider = context.RequestServices.GetRequiredService<ITraitsProvider>();
                    var effectiveTraitModel = context.RequestServices.GetRequiredService<IEffectiveTraitModel>();
                    var ciBasedAuthorizationService = context.RequestServices.GetRequiredService<ICIBasedAuthorizationService>();
                    var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var layerStrings = context.GetArgument<string[]>("layers");
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    // TODO: HORRIBLE performance!, consider aggressive caching
                    var traits = (await traitsProvider.GetActiveTraitSet(userContext.Transaction, userContext.TimeThreshold)).Traits;
                    var ret = new List<(string name, int count)>();
                    foreach (var trait in traits.Values)
                    {
                        var ets = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(trait, userContext.LayerSet, userContext.Transaction, userContext.TimeThreshold);
                        var readableETs = ets.Count(et => ciBasedAuthorizationService.CanReadCI(et.Key)); // CI based filtering
                        ret.Add((name: trait.Name, count: readableETs));
                    }
                    return ret;
                });

            Field<ListGraphType<StringGraphType>>("cacheKeys",
                resolve: context =>
                {
                    var memoryCacheModel = context.RequestServices.GetRequiredService<IMemoryCacheModel>();

                    var keys = memoryCacheModel.GetKeys();
                    return keys;
                });

            Field<ListGraphType<StringGraphType>>("debugCurrentUserClaims",
                resolve: context =>
                {
                    var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
                    var claims = currentUserService.DebugGetAllClaims();
                    return claims.Select(kv => $"{kv.type}: {kv.value}");
                });

            Field<VersionType>("version",
                resolve: context =>
                {
                    var loadedPlugins = context.RequestServices.GetServices<ILoadedPlugin>();
                    var coreVersion = VersionService.GetVersion();
                    return new VersionDTO(coreVersion, loadedPlugins);
                });
        }
    }
}
