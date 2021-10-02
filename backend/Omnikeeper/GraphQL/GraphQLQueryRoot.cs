using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Globalization;
using System.Linq;
using static Omnikeeper.Base.Model.IChangesetModel;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLQueryRoot : ObjectGraphType
    {
        private readonly ICIIDModel ciidModel;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly ILayerModel layerModel;
        private readonly ICISearchModel ciSearchModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IPredicateModel predicateModel;
        private readonly IChangesetModel changesetModel;
        private readonly ILayerStatisticsModel layerStatisticsModel;
        private readonly IGeneratorModel generatorModel;
        private readonly IOIAContextModel oiaContextModel;
        private readonly IODataAPIContextModel odataAPIContextModel;
        private readonly IAuthRoleModel authRoleModel;
        private readonly IRecursiveDataTraitModel recursiveDataTraitModel;
        private readonly ICurrentUserService currentUserService;
        private readonly IManagementAuthorizationService managementAuthorizationService;
        private readonly ICIBasedAuthorizationService ciBasedAuthorizationService;
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;


        public GraphQLQueryRoot(ICIIDModel ciidModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, ILayerModel layerModel,
            ICISearchModel ciSearchModel, ITraitsProvider traitsProvider, IBaseConfigurationModel baseConfigurationModel, IPredicateModel predicateModel,
            IChangesetModel changesetModel, ILayerStatisticsModel layerStatisticsModel, IGeneratorModel generatorModel,
            IOIAContextModel oiaContextModel, IODataAPIContextModel odataAPIContextModel, IAuthRoleModel authRoleModel,
            IRecursiveDataTraitModel recursiveDataTraitModel, ICurrentUserService currentUserService,
            IManagementAuthorizationService managementAuthorizationService,
            ICIBasedAuthorizationService ciBasedAuthorizationService, ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.ciidModel = ciidModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.layerModel = layerModel;
            this.ciSearchModel = ciSearchModel;
            this.traitsProvider = traitsProvider;
            this.baseConfigurationModel = baseConfigurationModel;
            this.predicateModel = predicateModel;
            this.changesetModel = changesetModel;
            this.layerStatisticsModel = layerStatisticsModel;
            this.generatorModel = generatorModel;
            this.oiaContextModel = oiaContextModel;
            this.odataAPIContextModel = odataAPIContextModel;
            this.authRoleModel = authRoleModel;
            this.recursiveDataTraitModel = recursiveDataTraitModel;
            this.currentUserService = currentUserService;
            this.managementAuthorizationService = managementAuthorizationService;
            this.ciBasedAuthorizationService = ciBasedAuthorizationService;
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;

            CreateMain();

            CreateManage();
        }

        private void CreateMain()
        {
            FieldAsync<ListGraphType<StringGraphType>>("ciids",
                resolve: async context =>
                {
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();

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
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    var withEffectiveTraits = context.GetArgument<string[]>("withEffectiveTraits", new string[0])!;
                    var withoutEffectiveTraits = context.GetArgument<string[]>("withoutEffectiveTraits", new string[0])!;
                    var layerStrings = context.GetArgument<string[]>("layers")!;
                    var ls = await layerModel.BuildLayerSet(layerStrings, userContext.Transaction);
                    userContext.LayerSet = ls;
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    userContext.TimeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest();

                    if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, ls))
                        throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', layerStrings)}");

                    // use ciids list to reduce the CIIDSelection
                    var searchString = context.GetArgument<string>("searchString", "")!;
                    var ciids = context.GetArgument<Guid[]?>("ciids", null);
                    ICIIDSelection ciidSelection = new AllCIIDsSelection();
                    if (ciids != null)
                    {
                        if (!ciBasedAuthorizationService.CanReadAllCIs(ciids, out var notAllowedCI))
                            throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read CI {notAllowedCI}");
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
                            var ciNames = await attributeModel.GetMergedCINames(ciidSelection, userContext.LayerSet, userContext.Transaction, userContext.TimeThreshold);
                            var foundCIIDs = ciNames.Where(kv => CultureInfo.InvariantCulture.CompareInfo.IndexOf(kv.Value, searchString, CompareOptions.IgnoreCase) >= 0).Select(kv => kv.Key).ToHashSet();
                            if (foundCIIDs.IsEmpty())
                                return new MergedCI[0];
                            ciidSelection = ciidSelection.Intersect(SpecificCIIDsSelection.Build(foundCIIDs));
                        }
                    }

                    // do a "forward" look into the graphql query to see which attributes we actually need to fetch to properly fulfill the request
                    // because we need to at least fetch a single attribute (due to internal reasons), we might as well fetch the name attribute and then don't care if it is requested or not
                    IAttributeSelection attributeSelection = NamedAttributesSelection.Build(ICIModel.NameAttribute);
                    //var needsNameAttribute = context.SubFields?.ContainsKey("name") ?? false;
                    if (context.SubFields != null && context.SubFields.TryGetValue("mergedAttributes", out var mergedAttributesField))
                    {
                        // check whether or not the attributeNames parameter was set, in which case we can reduce the attributes to query for
                        var attributeNamesArgument = mergedAttributesField.Arguments?.FirstOrDefault(a => a.Name == "attributeNames");
                        if (attributeNamesArgument != null && attributeNamesArgument.Value is ListValue lv)
                        {
                            var attributeNames = lv.Values.Select(v =>
                            {
                                if (v is StringValue sv)
                                    return sv.Value;
                                return null;
                            }).Where(v => v != null).Select(v => v!).ToHashSet();

                            attributeSelection = attributeSelection.Union(NamedAttributesSelection.Build(attributeNames));
                        } else
                        {
                            // we need to query all attributes
                            attributeSelection = AllAttributeSelection.Instance;
                        }
                    }

                    var requiredTraits = await traitsProvider.GetActiveTraitsByIDs(withEffectiveTraits, userContext.Transaction, userContext.TimeThreshold);
                    var requiredNonTraits = await traitsProvider.GetActiveTraitsByIDs(withoutEffectiveTraits, userContext.Transaction, userContext.TimeThreshold);
                    var cis = await ciSearchModel.FindMergedCIsByTraits(ciidSelection, attributeSelection, requiredTraits.Values, requiredNonTraits.Values, ls, userContext.Transaction, userContext.TimeThreshold);

                    // reduce CIs to those that are allowed
                    cis = ciBasedAuthorizationService.FilterReadableCIs(cis, (ci) => ci.ID);

                    // sort by name, if requested
                    var sortByCIName = context.GetArgument<bool>("sortByCIName", false)!;
                    if (sortByCIName)
                    {
                        // HACK, properly sort unnamed CIs
                        cis = cis.OrderBy(t => t.CIName ?? "ZZZZZZZZZZZ");
                    }

                    return cis;
                });

            FieldAsync<ListGraphType<PredicateType>>("predicates",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
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
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

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
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

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
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

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
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var traits = await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.TimeThreshold);
                    return traits.Values.OrderBy(t => t.ID);
                });

            FieldAsync<StatisticsType>("statistics",
                resolve: async context =>
                {
                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    userContext.Transaction = modelContextBuilder.BuildImmediate();
                    userContext.TimeThreshold = TimeThreshold.BuildLatest();

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var layers = await layerModel.GetLayers(userContext.Transaction); // TODO: we only need count, implement more efficient model method
                    var ciids = await ciidModel.GetCIIDs(userContext.Transaction);
                    var traits = await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.TimeThreshold);
                    var predicates = await predicateModel.GetPredicates(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, userContext.TimeThreshold); // TODO: implement PredicateProvider
                    var generators = await generatorModel.GetGenerators(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, userContext.TimeThreshold); // TODO: implement GeneratorProvider

                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributes(null, userContext.Transaction);
                    var numAttributeChanges = await layerStatisticsModel.GetAttributeChangesHistory(null, userContext.Transaction);
                    var numActiveRelations = await layerStatisticsModel.GetActiveRelations(null, userContext.Transaction);
                    var numRelationChanges = await layerStatisticsModel.GetRelationChangesHistory(null, userContext.Transaction);
                    var numChangesets = await changesetModel.GetNumberOfChangesets(userContext.Transaction);

                    return new Statistics(ciids.Count(), numActiveAttributes, numActiveRelations, numChangesets, numAttributeChanges, numRelationChanges, layers.Count(), traits.Count(), predicates.Count(), generators.Count());
                });
        }
    }
}
