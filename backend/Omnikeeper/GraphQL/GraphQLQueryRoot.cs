using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.TraitEntities;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using static Omnikeeper.Base.Model.IChangesetModel;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLQueryRoot : ObjectGraphType
    {
        private readonly ICIIDModel ciidModel;
        private readonly IAttributeModel attributeModel;
        private readonly ILayerModel layerModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly GenericTraitEntityModel<Predicate, string> predicateModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerStatisticsModel layerStatisticsModel;
        private readonly GenericTraitEntityModel<GeneratorV1, string> generatorModel;
        private readonly IOIAContextModel oiaContextModel;
        private readonly IODataAPIContextModel odataAPIContextModel;
        private readonly GenericTraitEntityModel<AuthRole, string> authRoleModel;
        private readonly GenericTraitEntityModel<CLConfigV1, string> clConfigModel;
        private readonly GenericTraitEntityModel<RecursiveTrait, string> recursiveDataTraitModel;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly ILatestLayerChangeModel latestLayerChangeModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IDataLoaderService dataLoaderService;

        public GraphQLQueryRoot(ICIIDModel ciidModel, IAttributeModel attributeModel, ILayerModel layerModel, ILayerDataModel layerDataModel, ICIModel ciModel, IEffectiveTraitModel effectiveTraitModel,
            ITraitsProvider traitsProvider, IMetaConfigurationModel metaConfigurationModel, GenericTraitEntityModel<Predicate, string> predicateModel,
            IChangesetModel changesetModel, ILayerStatisticsModel layerStatisticsModel, GenericTraitEntityModel<GeneratorV1, string> generatorModel, IBaseConfigurationModel baseConfigurationModel,
            IOIAContextModel oiaContextModel, IODataAPIContextModel odataAPIContextModel, GenericTraitEntityModel<AuthRole, string> authRoleModel, GenericTraitEntityModel<CLConfigV1, string> clConfigModel,
            GenericTraitEntityModel<RecursiveTrait, string> recursiveDataTraitModel, IManagementAuthorizationService managementAuthorizationService,
            IEnumerable<IPluginRegistration> plugins, ILatestLayerChangeModel latestLayerChangeModel, IBaseAttributeModel baseAttributeModel,
            ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerBasedAuthorizationService layerBasedAuthorizationService, IDataLoaderService dataLoaderService)
        {
            this.ciidModel = ciidModel;
            this.attributeModel = attributeModel;
            this.layerModel = layerModel;
            this.layerDataModel = layerDataModel;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.traitsProvider = traitsProvider;
            this.metaConfigurationModel = metaConfigurationModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.predicateModel = predicateModel;
            this.changesetModel = changesetModel;
            this.layerStatisticsModel = layerStatisticsModel;
            this.generatorModel = generatorModel;
            this.oiaContextModel = oiaContextModel;
            this.odataAPIContextModel = odataAPIContextModel;
            this.authRoleModel = authRoleModel;
            this.clConfigModel = clConfigModel;
            this.recursiveDataTraitModel = recursiveDataTraitModel;
            this.managementAuthorizationService = managementAuthorizationService;
            this.latestLayerChangeModel = latestLayerChangeModel;
            this.baseAttributeModel = baseAttributeModel;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.dataLoaderService = dataLoaderService;

            CreateMain();
            CreateManage();
            CreatePlugin(plugins);
        }

        private void CreateMain()
        {
            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var ciids = await ciidModel.GetCIIDs(userContext.Transaction);
                    // reduce CIs to those that are allowed
                    ciids = ciBasedAuthorizationService.FilterReadableCIs(ciids);
                    return ciids;
                });

            FieldAsync<ListGraphType<MergedCIType>>("cis",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<StringGraphType> { Name = "searchString" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withEffectiveTraits" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withoutEffectiveTraits" },
                    new QueryArgument<BooleanGraphType> { Name = "sortByCIName" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: async context =>
                {
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold((ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest(), context.Path)
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits", new string[0])!;
                    var withoutEffectiveTraits = context.GetArgument<string[]>("withoutEffectiveTraits", new string[0])!;

                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var layerSet = userContext.GetLayerSet(context.Path);

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, layerSet))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

                    // use ciids list to reduce the CIIDSelection
                    var searchString = context.GetArgument<string>("searchString", "")!;
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    ICIIDSelection ciidSelection = new AllCIIDsSelection();
                    if (ciids != null)
                    {
                        ciidSelection = SpecificCIIDsSelection.Build(ciids);
                    }

                    // use searchString to reduce the CIIDSelection further
                    var finalSS = searchString.Trim();
                    if (finalSS.Length > 0)
                    {
                        if (Guid.TryParse(finalSS, out var guid))
                        {
                            ciidSelection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(guid));
                        }
                        else
                        {
                            var ciNames = await attributeModel.GetMergedCINames(ciidSelection, layerSet, userContext.Transaction, timeThreshold);
                            var foundCIIDs = ciNames.Where(kv => CultureInfo.InvariantCulture.CompareInfo.IndexOf(kv.Value, searchString, CompareOptions.IgnoreCase) >= 0).Select(kv => kv.Key).ToHashSet();
                            if (foundCIIDs.IsEmpty())
                                return Array.Empty<MergedCI>();
                            ciidSelection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(foundCIIDs));
                        }
                    }

                    bool preAuthzCheckedCIs = false;
                    if (ciidSelection is SpecificCIIDsSelection specificCIIDsSelection)
                    {
                        if (!ciBasedAuthorizationService.CanReadAllCIs(specificCIIDsSelection.CIIDs, out var notAllowedCI))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {notAllowedCI}");
                        preAuthzCheckedCIs = true;
                    }

                    var requiredTraits = await traitsProvider.GetActiveTraitsByIDs(withEffectiveTraits, userContext.Transaction, timeThreshold);
                    var requiredNonTraits = await traitsProvider.GetActiveTraitsByIDs(withoutEffectiveTraits, userContext.Transaction, timeThreshold);

                    IAttributeSelection attributeSelection = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, userContext.Transaction, timeThreshold);

                    // create shallow copy, because we potentially modify these lists
                    IEnumerable<ITrait> requiredTraitsCopy = new List<ITrait>(requiredTraits.Values);
                    IEnumerable<ITrait> requiredNonTraitsCopy = new List<ITrait>(requiredNonTraits.Values);
                    if (effectiveTraitModel.ReduceTraitRequirements(ref requiredTraitsCopy, ref requiredNonTraitsCopy, out var emptyTraitIsRequired, out var emptyTraitIsNonRequired))
                        return Array.Empty<MergedCI>(); // bail completely

                    // special case: empty trait is required
                    if (emptyTraitIsRequired)
                    {
                        // TODO: better performance possible if we get empty CIIDs and exclude those?
                        var nonEmptyCIIDs = await baseAttributeModel.GetCIIDsWithAttributes(ciidSelection, layerSet.LayerIDs, userContext.Transaction, timeThreshold);
                        var emptyCIIDSelection = ciidSelection.Except(SpecificCIIDsSelection.Build(nonEmptyCIIDs));
                        var emptyCIIDs = await emptyCIIDSelection.GetCIIDsAsync(async () => await ciModel.GetCIIDs(userContext.Transaction));
                        return emptyCIIDs.Select(ciid => new MergedCI(ciid, null, layerSet, timeThreshold, ImmutableDictionary<string, MergedCIAttribute>.Empty));
                    }

                    // reduce attribute selection, where possible
                    var relevantAttributesForTraits = requiredTraitsCopy.SelectMany(t => t.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name)).Concat(
                        requiredNonTraitsCopy.SelectMany(t => t.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name))
                        ).ToHashSet();

                    // NOTE: the attributeSelection is supposed to determine what gets RETURNED, not what attributes are checked against when testing for trait memberships
                    // NOTE: so, because it needs to check for traits it fetches more attributes, and hence this method may return more attributes than requested
                    var finalAttributeSelection = attributeSelection.Union(NamedAttributesSelection.Build(relevantAttributesForTraits));

                    // TODO: includeEmptyCIs is slow, and most often, not necessary: find way to get rid of it
                    var workCIs = await ciModel.GetMergedCIs(ciidSelection, layerSet, includeEmptyCIs: true, finalAttributeSelection, userContext.Transaction, timeThreshold);

                    // in case the empty trait is non-required, we reduce the workCIs list by those CIs that are empty
                    // we could also have done this by reducing the CIIDSelection first, but this has worse performance for most typical use-cases
                    // because it produces a SpecificCIIDSelection with a huge list
                    if (emptyTraitIsNonRequired)
                    {
                        // TODO: better performance possible if we get empty CIIDs and exclude those?
                        var nonEmptyCIIDs = await baseAttributeModel.GetCIIDsWithAttributes(ciidSelection, layerSet.LayerIDs, userContext.Transaction, timeThreshold);
                        workCIs = workCIs.Where(ci => nonEmptyCIIDs.Contains(ci.ID));
                    }

                    var cisFilteredByTraits = effectiveTraitModel.FilterMergedCIsByTraits(workCIs, requiredTraitsCopy, requiredNonTraitsCopy, layerSet, userContext.Transaction, timeThreshold);

                    // reduce CIs to those that are allowed
                    if (!preAuthzCheckedCIs)
                        cisFilteredByTraits = ciBasedAuthorizationService.FilterReadableCIs(cisFilteredByTraits, (ci) => ci.ID);

                    // sort by name, if requested
                    var sortByCIName = context.GetArgument<bool>("sortByCIName", false)!;
                    if (sortByCIName)
                    {
                        // HACK, properly sort unnamed CIs
                        cisFilteredByTraits = cisFilteredByTraits.OrderBy(t => t.CIName ?? "ZZZZZZZZZZZ");
                    }

                    return cisFilteredByTraits;
                });

            FieldAsync<DiffingResultType>("ciDiffing",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "leftLayers" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "rightLayers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "leftTimeThreshold" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "rightTimeThreshold" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "leftCIIDs" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "rightCIIDs" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "leftAttributes" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "rightAttributes" },
                    new QueryArgument<BooleanGraphType> { Name = "showEqual" },
                    new QueryArgument<BooleanGraphType> { Name = "allowCrossCIDiffing" }
                    ),
                resolve: async context =>
                {
                    var userContext = context
                        .SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var leftLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"leftLayers")!, userContext.Transaction);
                    var rightLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"rightLayers")!, userContext.Transaction);
                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, leftLayers))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', leftLayers.LayerIDs)}");
                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, rightLayers))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', rightLayers.LayerIDs)}");
                    
                    var leftTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"leftTimeThreshold");
                    var leftTimeThreshold = (!leftTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(leftTimeThresholdDTO.Value);
                    var rightTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"rightTimeThreshold");
                    var rightTimeThreshold = (!rightTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(rightTimeThresholdDTO.Value);
                    
                    var leftCIIDs = context.GetArgument<Guid[]?>($"leftCIIDs", null);
                    var rightCIIDs = context.GetArgument<Guid[]?>($"rightCIIDs", null);
                    
                    var leftAttributes = context.GetArgument<string[]?>($"leftAttributes", null);
                    var rightAttributes = context.GetArgument<string[]?>($"rightAttributes", null);
                    IAttributeSelection leftAttributeSelection = AllAttributeSelection.Instance;
                    if (leftAttributes != null) leftAttributeSelection = NamedAttributesSelection.Build(leftAttributes);
                    IAttributeSelection rightAttributeSelection = AllAttributeSelection.Instance;
                    if (rightAttributes != null) rightAttributeSelection = NamedAttributesSelection.Build(rightAttributes);

                    ICIIDSelection leftCIIDSelection = (leftCIIDs != null) ? SpecificCIIDsSelection.Build(leftCIIDs) : new AllCIIDsSelection();
                    ICIIDSelection rightCIIDSelection = (rightCIIDs != null) ? SpecificCIIDsSelection.Build(rightCIIDs) : new AllCIIDsSelection();

                    var showEqual = context.GetArgument<bool?>($"showEqual").GetValueOrDefault(true);

                    // if it's allowed AND there's one CI on each side AND the CIs are different, switch to "crossCIDiffing"
                    var allowCrossCIDiffing = context.GetArgument<bool?>($"allowCrossCIDiffing").GetValueOrDefault(true);
                    var crossCIDiffingSettings = (allowCrossCIDiffing && leftCIIDs?.Length == 1 && rightCIIDs?.Length == 1 && leftCIIDs[0] != rightCIIDs[0]) ? 
                        new CrossCIDiffingSettings(leftCIIDs[0], rightCIIDs[0]) :
                        null;

                    // NOTE: because many sub-resolvers of ciDiffing depend on whether they are "left" or "right", we set some parts of the user context (layerset, timethreshold) depending on that
                    // create sub contexts for left and right branches of graphql query
                    // TODO: simplify interface, allow multiple paths to be added in one call to With*()
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "cis", "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "cis", "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "cis", "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "cis", "right" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "cis", "attributeComparisons", "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "cis", "attributeComparisons", "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "cis", "attributeComparisons", "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "cis", "attributeComparisons", "right" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "outgoingRelations", "relationComparisons", "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "outgoingRelations", "relationComparisons", "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "outgoingRelations", "relationComparisons", "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "outgoingRelations", "relationComparisons", "right" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "incomingRelations", "relationComparisons", "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "incomingRelations", "relationComparisons", "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "incomingRelations", "relationComparisons", "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "incomingRelations", "relationComparisons", "right" }));

                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "outgoingRelations", "leftCIName" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "outgoingRelations", "leftCIName" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "incomingRelations", "leftCIName" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "incomingRelations", "leftCIName" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "outgoingRelations", "rightCIName" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "outgoingRelations", "rightCIName" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "incomingRelations", "rightCIName" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "incomingRelations", "rightCIName" }));

                    return new DiffingResult(leftCIIDSelection, rightCIIDSelection, leftAttributeSelection, rightAttributeSelection, leftLayers, rightLayers, leftTimeThreshold, rightTimeThreshold, showEqual, crossCIDiffingSettings);
                });

            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }
                    ),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(context.GetArgument("timeThreshold", TimeThreshold.BuildLatest()), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    var predicates = await predicateModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return predicates.Values;
                });

            Field<ListGraphType<LayerDataType>>("layers",
                arguments: new QueryArguments(
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }
                    ),
                resolve: context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(context.GetArgument("timeThreshold", TimeThreshold.BuildLatest()), context.Path);

                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layersDict =>
                        {
                            // authz filter
                            return layersDict.Values.Where(l => layerBasedAuthorizationService.CanUserReadFromLayer(userContext.User, l.LayerID));
                        });
                });

            FieldAsync<ChangesetType>("changeset",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    var id = context.GetArgument<Guid>("id");

                    // TODO: use dataloader
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
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, userContext.GetLayerSet(context.Path)))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    var limit = context.GetArgument<int?>("limit", null);
                    IChangesetSelection selection = new ChangesetSelectionAllCIs();
                    if (ciids != null)
                        selection = ChangesetSelectionSpecificCIs.Build(ciids);

                    // NOTE: we can't filter the changesets using CIBasedAuthorizationService because changesets are not bound to CIs

                    return await changesetModel.GetChangesetsInTimespan(from, to, userContext.GetLayerSet(context.Path), selection, userContext.Transaction, limit);
                });

            FieldAsync<TraitType>("activeTrait",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var id = context.GetArgument<string>("id")!;

                    var trait = await traitsProvider.GetActiveTrait(id, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    return trait;
                });

            FieldAsync<ListGraphType<TraitType>>("activeTraits",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var traits = await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    return traits.Values.OrderBy(t => t.ID);
                });

            FieldAsync<ListGraphType<EffectiveTraitType>>("effectiveTraitsForTrait",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "traitID" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" }),
                resolve: async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var traitID = context.GetArgument<string>("traitID")!;
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);

                    var userContext = await context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    ICIIDSelection ciidSelection = new AllCIIDsSelection();
                    if (ciids != null)
                    {
                        ciidSelection = SpecificCIIDsSelection.Build(ciids);
                    }

                    var trait = await traitsProvider.GetActiveTrait(traitID, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (trait == null)
                        throw new ExecutionError($"No trait with ID {traitID} found");

                    var relevantAttributesForTrait = trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name)
                        .Concat(
                            trait.OptionalAttributes.Select(ra => ra.AttributeTemplate.Name)
                        ).ToHashSet();

                    var cis = await ciModel.GetMergedCIs(ciidSelection, userContext.GetLayerSet(context.Path), false, NamedAttributesSelection.Build(relevantAttributesForTrait), userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    var ets = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return ets.Values;
                });


            FieldAsync<StatisticsType>("statistics",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var layers = await layerModel.GetLayers(userContext.Transaction); // TODO: we only need count, implement more efficient model method
                    var ciids = await ciidModel.GetCIIDs(userContext.Transaction);
                    var traits = await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    var predicates = await predicateModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path)); // TODO: implement PredicateProvider
                    var generators = await generatorModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path)); // TODO: implement GeneratorProvider

                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributes(null, userContext.Transaction);
                    var numAttributeChanges = await layerStatisticsModel.GetAttributeChangesHistory(null, userContext.Transaction);
                    var numActiveRelations = await layerStatisticsModel.GetActiveRelations(null, userContext.Transaction);
                    var numRelationChanges = await layerStatisticsModel.GetRelationChangesHistory(null, userContext.Transaction);
                    var numChangesets = await changesetModel.GetNumberOfChangesets(userContext.Transaction);

                    return new Statistics(ciids.Count(), numActiveAttributes, numActiveRelations, numChangesets, numAttributeChanges, numRelationChanges, layers.Count(), traits.Count(), predicates.Count(), generators.Count());
                });

            FieldAsync<TraitEntitiesType>("traitEntities",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" }),
                resolve: async context =>
            {
                var layerStrings = context.GetArgument<string[]>("layers")!;

                var userContext = await context.SetupUserContext()
                    .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path)
                    .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                    .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                return new TraitEntities.TraitEntities();
            });
        }

        private void CreatePlugin(IEnumerable<IPluginRegistration> plugins)
        {
            foreach(var plugin in plugins)
            {
                plugin.RegisterGraphqlQueries(this);
            }
        }
    }
}
