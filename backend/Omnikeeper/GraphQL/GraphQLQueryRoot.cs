﻿using Autofac.Features.Indexed;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.TraitEntities;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Service;
using Quartz;
using System;
using System.Collections;
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
        private readonly ITraitsHolder traitsHolder;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerStatisticsModel layerStatisticsModel;
        private readonly GeneratorV1Model generatorModel;
        private readonly AuthRoleModel authRoleModel;
        private readonly CLConfigV1Model clConfigModel;
        private readonly ValidatorContextV1Model validatorContextModel;
        private readonly RecursiveTraitModel recursiveDataTraitModel;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly IUserInDatabaseModel userInDatabaseModel;
        private readonly IAuthzFilterManager authzFilterManager;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly IScheduler localScheduler;
        private readonly IScheduler distributedScheduler;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;
        private readonly IDataLoaderService dataLoaderService;

        public GraphQLQueryRoot(ICIIDModel ciidModel, IAttributeModel attributeModel, ILayerModel layerModel, ILayerDataModel layerDataModel, ICIModel ciModel, IEffectiveTraitModel effectiveTraitModel,
            ITraitsHolder traitsHolder, IMetaConfigurationModel metaConfigurationModel, 
            IChangesetModel changesetModel, ILayerStatisticsModel layerStatisticsModel, GeneratorV1Model generatorModel, IBaseConfigurationModel baseConfigurationModel,
            AuthRoleModel authRoleModel, CLConfigV1Model clConfigModel,
            RecursiveTraitModel recursiveDataTraitModel, IManagementAuthorizationService managementAuthorizationService,
            IUserInDatabaseModel userInDatabaseModel, IAuthzFilterManager authzFilterManager,
            IEnumerable<IPluginRegistration> plugins, IBaseAttributeModel baseAttributeModel, IIndex<string, IScheduler> schedulers,
            ILayerBasedAuthorizationService layerBasedAuthorizationService, IDataLoaderService dataLoaderService, ValidatorContextV1Model validatorContextModel)
        {
            this.ciidModel = ciidModel;
            this.attributeModel = attributeModel;
            this.layerModel = layerModel;
            this.layerDataModel = layerDataModel;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.traitsHolder = traitsHolder;
            this.metaConfigurationModel = metaConfigurationModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.changesetModel = changesetModel;
            this.layerStatisticsModel = layerStatisticsModel;
            this.generatorModel = generatorModel;
            this.authRoleModel = authRoleModel;
            this.clConfigModel = clConfigModel;
            this.recursiveDataTraitModel = recursiveDataTraitModel;
            this.managementAuthorizationService = managementAuthorizationService;
            this.userInDatabaseModel = userInDatabaseModel;
            this.authzFilterManager = authzFilterManager;
            this.baseAttributeModel = baseAttributeModel;
            this.localScheduler = schedulers["localScheduler"];
            this.distributedScheduler = schedulers["distributedScheduler"];
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
            this.dataLoaderService = dataLoaderService;
            this.validatorContextModel = validatorContextModel;

            CreateMain();
            CreateManage();
            CreatePlugin(plugins);
        }

        private void CreateMain()
        {
            Field<ListGraphType<GuidGraphType>>("ciids")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var ciids = await ciidModel.GetCIIDs(userContext.Transaction);
                    return ciids;
                });

            Field<ListGraphType<MergedCIType>>("cis")
                .Arguments(
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<StringGraphType> { Name = "searchString" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withEffectiveTraits" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "withoutEffectiveTraits" },
                    new QueryArgument<BooleanGraphType> { Name = "sortByCIName" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" })
                .ResolveAsync(async context =>
                {
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.GetUserContext()
                        .WithTimeThreshold((ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest(), context.Path)
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits", new string[0])!;
                    var withoutEffectiveTraits = context.GetArgument<string[]>("withoutEffectiveTraits", new string[0])!;

                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var layerSet = userContext.GetLayerSet(context.Path);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, layerSet, userContext.Transaction, timeThreshold) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    // use ciids list to reduce the CIIDSelection
                    var searchString = context.GetArgument<string>("searchString", "")!;
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    ICIIDSelection ciidSelection = AllCIIDsSelection.Instance;
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
                            // TODO: use dataloader
                            var ciNames = await attributeModel.GetMergedCINames(ciidSelection, layerSet, userContext.Transaction, timeThreshold);
                            var foundCIIDs = ciNames.Where(kv => CultureInfo.InvariantCulture.CompareInfo.IndexOf(kv.Value, searchString, CompareOptions.IgnoreCase) >= 0).Select(kv => kv.Key).ToHashSet();
                            if (foundCIIDs.IsEmpty())
                                return Array.Empty<MergedCI>();
                            ciidSelection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(foundCIIDs));
                        }
                    }

                    var requiredTraits = traitsHolder.GetTraits(withEffectiveTraits);
                    var requiredNonTraits = traitsHolder.GetTraits(withoutEffectiveTraits);

                    IAttributeSelection attributeSelection = MergedCIType.ForwardInspectRequiredAttributes(context, traitsHolder, userContext.Transaction, timeThreshold);

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
                    IEnumerable<MergedCI> workCIs = await ciModel.GetMergedCIs(ciidSelection, layerSet, includeEmptyCIs: true, finalAttributeSelection, userContext.Transaction, timeThreshold);

                    // in case the empty trait is non-required, we reduce the workCIs list by those CIs that are empty
                    // we could also have done this by reducing the CIIDSelection first, but this has worse performance for most typical use-cases
                    // because it produces a SpecificCIIDSelection with a huge list
                    if (emptyTraitIsNonRequired)
                    {
                        // TODO: better performance possible if we get empty CIIDs and exclude those?
                        var nonEmptyCIIDs = await baseAttributeModel.GetCIIDsWithAttributes(ciidSelection, layerSet.LayerIDs, userContext.Transaction, timeThreshold);
                        workCIs = workCIs.Where(ci => nonEmptyCIIDs.Contains(ci.ID));
                    }

                    var cisFilteredByTraits = effectiveTraitModel.FilterMergedCIsByTraits(workCIs, requiredTraitsCopy, requiredNonTraitsCopy, layerSet);

                    // sort by name, if requested
                    var sortByCIName = context.GetArgument<bool>("sortByCIName", false)!;
                    if (sortByCIName)
                    {
                        // HACK, properly sort unnamed CIs
                        cisFilteredByTraits = cisFilteredByTraits.OrderBy(t => t.CIName ?? "ZZZZZZZZZZZ");
                    }

                    return cisFilteredByTraits;
                });

            Field<DiffingResultType>("ciDiffing")
                .Arguments(
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
                    )
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var leftLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"leftLayers")!, userContext.Transaction);
                    var rightLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"rightLayers")!, userContext.Transaction);

                    var leftTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"leftTimeThreshold");
                    var leftTimeThreshold = (!leftTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(leftTimeThresholdDTO.Value);
                    var rightTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"rightTimeThreshold");
                    var rightTimeThreshold = (!rightTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(rightTimeThresholdDTO.Value);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, leftLayers, userContext.Transaction, leftTimeThreshold) is AuthzFilterResultDeny dLeft)
                        throw new ExecutionError(dLeft.Reason);
                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, rightLayers, userContext.Transaction, rightTimeThreshold) is AuthzFilterResultDeny dRight)
                        throw new ExecutionError(dRight.Reason);

                    var leftCIIDs = context.GetArgument<Guid[]?>($"leftCIIDs", null);
                    var rightCIIDs = context.GetArgument<Guid[]?>($"rightCIIDs", null);

                    var leftAttributes = context.GetArgument<string[]?>($"leftAttributes", null);
                    var rightAttributes = context.GetArgument<string[]?>($"rightAttributes", null);
                    IAttributeSelection leftAttributeSelection = AllAttributeSelection.Instance;
                    if (leftAttributes != null) leftAttributeSelection = NamedAttributesSelection.Build(leftAttributes);
                    IAttributeSelection rightAttributeSelection = AllAttributeSelection.Instance;
                    if (rightAttributes != null) rightAttributeSelection = NamedAttributesSelection.Build(rightAttributes);

                    ICIIDSelection leftCIIDSelection = (leftCIIDs != null) ? SpecificCIIDsSelection.Build(leftCIIDs) : AllCIIDsSelection.Instance;
                    ICIIDSelection rightCIIDSelection = (rightCIIDs != null) ? SpecificCIIDsSelection.Build(rightCIIDs) : AllCIIDsSelection.Instance;

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

            Field<ListGraphType<LayerDataType>>("layers")
                .Arguments(
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }
                    )
                .Resolve(context =>
                {
                    var userContext = context.GetUserContext()
                        .WithTimeThreshold(context.GetArgument("timeThreshold", TimeThreshold.BuildLatest()), context.Path);

                    return dataLoaderService.SetupAndLoadAllLayers(userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layersDict =>
                        {
                            // authz filter
                            return layersDict.Values.Where(l => layerBasedAuthorizationService.CanUserReadFromLayer(userContext.User, l.LayerID));
                        });
                });

            Field<ChangesetType>("changeset")
                .Arguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" })
                .ResolveAsync(async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    var id = context.GetArgument<Guid>("id");

                    // TODO: use dataloader
                    var changeset = await changesetModel.GetChangeset(id, userContext.Transaction);
                    return changeset;
                });

            Field<ListGraphType<ChangesetType>>("changesets")
                .Arguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>> { Name = "from" },
                    new QueryArgument<NonNullGraphType<DateTimeOffsetGraphType>> { Name = "to" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" },
                    new QueryArgument<IntGraphType> { Name = "limit" })
                .ResolveAsync(async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    var from = context.GetArgument<DateTimeOffset>("from");
                    var to = context.GetArgument<DateTimeOffset>("to");
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    var limit = context.GetArgument<int?>("limit", null);
                    IChangesetSelection selection = new ChangesetSelectionAllCIs();
                    if (ciids != null)
                        selection = ChangesetSelectionSpecificCIs.Build(ciids);

                    return await changesetModel.GetChangesetsInTimespan(from, to, userContext.GetLayerSet(context.Path).LayerIDs, selection, userContext.Transaction, limit);
                });

            Field<ChangesetType>("latestChangeset")
                .Arguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" })
                .ResolveAsync(async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    var latestChangeset = await changesetModel.GetLatestChangesetOverall(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, PredicateSelectionAll.Instance, userContext.GetLayerSet(context.Path).LayerIDs, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return latestChangeset;
                });

            Field<TraitType>("activeTrait")
                .Arguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" })
                .Resolve(context =>
                {
                    var userContext = context.GetUserContext();

                    var id = context.GetArgument<string>("id")!;

                    var trait = traitsHolder.GetTrait(id);
                    return trait;
                });

            Field<ListGraphType<TraitType>>("activeTraits")
                .Resolve(context =>
                {
                    var userContext = context.GetUserContext();

                    var traits = traitsHolder.GetTraits();
                    return traits.Values.OrderBy(t => t.ID);
                });



            Field<BooleanGraphType>("checkTrait")
              .Arguments(
                new QueryArgument<NonNullGraphType<UpsertRecursiveTraitInputType>> { Name = "trait" }
              )
              .Resolve(context =>
              {
                  var userContext = context.GetUserContext();

                  var trait = context.GetArgument<UpsertRecursiveTraitInput>("trait")!;

                  var existingTraits = traitsHolder.GetTraits();

                  if (!existingTraits.TryGetValue(trait.ID, out var existingTrait))
                      return false;

                  if (existingTrait is not GenericTrait existingGenericTrait) // cannot compare non-generic trait
                      throw new ExecutionError("Cannot check against non-generic trait");

                  // NOTE: we don't compare Origin, because the trait can be defined anywhere
                  return existingGenericTrait.ID == trait.ID
                    && StructuralComparisons.StructuralEqualityComparer.Equals(existingGenericTrait.RequiredAttributes, trait.RequiredAttributes)
                    && StructuralComparisons.StructuralEqualityComparer.Equals(existingGenericTrait.OptionalAttributes, trait.OptionalAttributes)
                    && StructuralComparisons.StructuralEqualityComparer.Equals(existingGenericTrait.OptionalRelations, trait.OptionalRelations)
                    && StructuralComparisons.StructuralEqualityComparer.Equals(existingGenericTrait.AncestorTraits, trait.RequiredTraits)
                    ;
              });

            Field<ListGraphType<EffectiveTraitType>>("effectiveTraitsForTrait")
                .Arguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "traitID" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "ciids" })
                .ResolveAsync(async context =>
                {
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var traitID = context.GetArgument<string>("traitID")!;
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);

                    var userContext = await context.GetUserContext()
                        .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                    if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                        throw new ExecutionError(d.Reason);

                    ICIIDSelection ciidSelection = AllCIIDsSelection.Instance;
                    if (ciids != null)
                    {
                        ciidSelection = SpecificCIIDsSelection.Build(ciids);
                    }

                    var trait = traitsHolder.GetTrait(traitID);
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


            Field<StatisticsType>("statistics")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var layers = await layerModel.GetLayers(userContext.Transaction); // TODO: we only need count, implement more efficient model method
                    var traits = traitsHolder.GetTraits();
                    var generators = await generatorModel.GetByCIID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path)); // TODO: implement GeneratorProvider

                    var numCIIDs = await layerStatisticsModel.GetCIIDsApproximate(userContext.Transaction);
                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributesApproximate(userContext.Transaction);
                    var numAttributeChanges = await layerStatisticsModel.GetAttributeChangesHistoryApproximate(userContext.Transaction);
                    var numActiveRelations = await layerStatisticsModel.GetActiveRelationsApproximate(userContext.Transaction);
                    var numRelationChanges = await layerStatisticsModel.GetRelationChangesHistoryApproximate(userContext.Transaction);
                    var numChangesets = await changesetModel.GetNumberOfChangesets(userContext.Transaction);

                    return new Statistics(numCIIDs, numActiveAttributes, numActiveRelations, numChangesets, numAttributeChanges, numRelationChanges, layers.Count(), traits.Count(), generators.Count());
                });

            Field<TraitEntitiesType>("traitEntities")
                .Arguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "layers" })
                .ResolveAsync(async context =>
            {
                var layerStrings = context.GetArgument<string[]>("layers")!;

                var userContext = await context.GetUserContext()
                    .WithLayersetAsync(async trans => await layerModel.BuildLayerSet(layerStrings, trans), context.Path);

                if (await authzFilterManager.ApplyFilterForQuery(new QueryOperationContext(), userContext.User, userContext.GetLayerSet(context.Path), userContext.Transaction, userContext.GetTimeThreshold(context.Path)) is AuthzFilterResultDeny d)
                    throw new ExecutionError(d.Reason);

                return new TraitEntities.TraitEntities();
            });

            Field<MyUserType>("myUser")
                .Resolve(context => new MyUser());
        }

        private void CreatePlugin(IEnumerable<IPluginRegistration> plugins)
        {
            foreach (var plugin in plugins)
            {
                plugin.RegisterGraphqlQueries(this);
            }
        }
    }
}
