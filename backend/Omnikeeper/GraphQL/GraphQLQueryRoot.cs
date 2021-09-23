using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Linq;
using static Omnikeeper.Base.Model.IChangesetModel;
namespace Omnikeeper.GraphQL
{
    public partial class GraphQLQueryRoot : ObjectGraphType
    {
        public GraphQLQueryRoot()
        {
            CreateMain();

            CreateManage();
        }

        private void CreateMain()
        {
            FieldAsync<MergedCIType>("ci",
                   arguments: new QueryArguments(
                       new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" },
                       new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                       new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                   resolve: async context =>
                   {
                       var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                       var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                       var ciBasedAuthorizationService = context.RequestServices!.GetRequiredService<ICIBasedAuthorizationService>();
                       var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                       var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                       var userContext = (context.UserContext as OmnikeeperUserContext)!;
                       userContext.Transaction = modelContextBuilder.BuildImmediate();
                       var ciid = context.GetArgument<Guid>("ciid");
                       var layerStrings = context.GetArgument<string[]>("layers")!;
                       var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                       userContext.LayerSet = ls;
                       var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                       userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                       if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, ls))
                           throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");
                       if (!ciBasedAuthorizationService.CanReadCI(ciid))
                           throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {ciid}");

                       // TODO: reduce attribute selection when mergedAttributes sub-field parameter "attributeNames" is chosen
                       var ci = await ciModel.GetMergedCI(ciid, userContext.LayerSet, AllAttributeSelection.Instance, userContext.Transaction, userContext.TimeThreshold);

                       return ci;
                   });
            FieldAsync<ListGraphType<MergedCIType>>("cis",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                    var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                    var ciBasedAuthorizationService = context.RequestServices!.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, ls))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");
                    ICIIDSelection ciidSelection = new AllCIIDsSelection();  // if null, query all CIs
                    if (ciids != null)
                    {
                        if (!ciBasedAuthorizationService.CanReadAllCIs(ciids, out var notAllowedCI))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {notAllowedCI}");
                        ciidSelection = SpecificCIIDsSelection.Build(ciids);
                    }

                    // TODO: reduce attribute selection when mergedAttributes sub-field parameter "attributeNames" is chosen
                    var cis = await ciModel.GetMergedCIs(ciidSelection, userContext.LayerSet, false, AllAttributeSelection.Instance, userContext.Transaction, userContext.TimeThreshold);

                    if (ciidSelection is AllCIIDsSelection)
                    {
                        // reduce CIs to those that are allowed
                        cis = ciBasedAuthorizationService.FilterReadableCIs(cis, (ci) => ci.ID);
                    }

                    return cis;
                });

            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var ciidModel = context.RequestServices!.GetRequiredService<ICIIDModel>();
                    var ciBasedAuthorizationService = context.RequestServices!.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var ciids = await ciidModel.GetCIIDs(userContext.Transaction);
                    // reduce CIs to those that are allowed
                    ciids = ciBasedAuthorizationService.FilterReadableCIs(ciids);
                    return ciids;
                });

            FieldAsync<ListGraphType<CompactCIType>>("advancedSearchCompactCIs",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "searchString" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withEffectiveTraits" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withoutEffectiveTraits" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                    var ciSearchModel = context.RequestServices!.GetRequiredService<ICISearchModel>();
                    var ciBasedAuthorizationService = context.RequestServices!.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var searchString = context.GetArgument<string>("searchString")!;
                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits", new string[0])!;
                    var withoutEffectiveTraits = context.GetArgument<string[]>("withoutEffectiveTraits", new string[0])!;
                    var ciid = context.GetArgument<Guid>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, ls))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

                    var cis = await ciSearchModel.AdvancedSearchForCompactCIs(searchString, withEffectiveTraits, withoutEffectiveTraits, ls, userContext.Transaction, userContext.TimeThreshold);
                    // reduce CIs to those that are allowed
                    cis = ciBasedAuthorizationService.FilterReadableCIs(cis, (ci) => ci.ID);
                    return cis;
                });

            FieldAsync<ListGraphType<MergedCIType>>("advancedSearchFullCIs",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withEffectiveTraits" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withoutEffectiveTraits" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                    var ciSearchModel = context.RequestServices!.GetRequiredService<ICISearchModel>();
                    var ciBasedAuthorizationService = context.RequestServices!.GetRequiredService<ICIBasedAuthorizationService>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits", new string[0])!;
                    var withoutEffectiveTraits = context.GetArgument<string[]>("withoutEffectiveTraits", new string[0])!;
                    var ciid = context.GetArgument<Guid>("identity");
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, ls))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

                    var cis = await ciSearchModel.SearchForMergedCIsByTraits(new AllCIIDsSelection(), withEffectiveTraits, withoutEffectiveTraits, ls, userContext.Transaction, userContext.TimeThreshold);
                    // reduce CIs to those that are allowed
                    cis = ciBasedAuthorizationService.FilterReadableCIs(cis, (ci) => ci.ID);
                    return cis;
                });

            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
                    var baseConfigurationModel = context.RequestServices!.GetRequiredService<IBaseConfigurationModel>();
                    var predicateModel = context.RequestServices!.GetRequiredService<IPredicateModel>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = context.GetArgument("timeThreshold", TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    var predicates = (await predicateModel.GetPredicates(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, userContext.TimeThreshold)).Values;

                    return predicates;
                });

            FieldAsync<ListGraphType<LayerType>>("layers",
                resolve: async context =>
                {
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

                    var layers = await layerModel.GetLayers(userContext.Transaction);

                    // authz filter
                    layers = layers.Where(l => layerBasedAuthorizationService.CanUserReadFromLayer(userContext.User, l));

                    return layers;
                });

            FieldAsync<ChangesetType>("changeset",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
                {
                    var changesetModel = context.RequestServices!.GetRequiredService<IChangesetModel>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
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
                    var changesetModel = context.RequestServices!.GetRequiredService<IChangesetModel>();
                    var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var layerBasedAuthorizationService = context.RequestServices!.GetRequiredService<ILayerBasedAuthorizationService>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    userContext.LayerSet = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, userContext.LayerSet))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

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

            FieldAsync<TraitType>("activeTrait",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }),
                resolve: async context =>
                {
                    var traitsProvider = context.RequestServices!.GetRequiredService<ITraitsProvider>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var id = context.GetArgument<string>("id")!;

                    var trait = await traitsProvider.GetActiveTrait(id, userContext.Transaction, userContext.TimeThreshold);
                    return trait;
                });

            FieldAsync<ListGraphType<TraitType>>("activeTraits",
                resolve: async context =>
                {
                    var traitsProvider = context.RequestServices!.GetRequiredService<ITraitsProvider>();
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var traits = await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.TimeThreshold);
                    return traits.Values.OrderBy(t => t.ID);
                });
        }
    }
}
