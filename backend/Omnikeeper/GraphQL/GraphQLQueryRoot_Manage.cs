using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Service;
using System;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public partial class GraphQLQueryRoot
    {
        private void CheckManagementPermissionThrow(OmnikeeperUserContext userContext, string reasonForCheck = "access management")
        {
            if (!managementAuthorizationService.HasManagementPermission(userContext.User))
                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to {reasonForCheck}");
        }

        private void CheckReadManagementThrow(OmnikeeperUserContext userContext, MetaConfiguration metaConfiguration, string reasonForCheck)
        {
            if (!managementAuthorizationService.CanReadManagement(userContext.User, metaConfiguration, out var message))
                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to {reasonForCheck}: {message}");
        }

        private void CreateManage()
        {
            Field<ListGraphType<LayerDataType>>("manage_layers",
                resolve: context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    CheckManagementPermissionThrow(userContext);

                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layersDict => layersDict.Values);
                });

            FieldAsync<LayerStatisticsType>("manage_layerStatistics",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "layerID" }),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var layerID = context.GetArgument<string>("layerID")!;

                    var layerData = await layerDataModel.GetLayerData(layerID, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (layerData == null)
                        throw new Exception($"Could not get layer with ID {layerID}");

                    CheckManagementPermissionThrow(userContext);

                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributes(layerData.LayerID, userContext.Transaction);
                    var numAttributeChangesHistory = await layerStatisticsModel.GetAttributeChangesHistory(layerData.LayerID, userContext.Transaction);
                    var numActiveRelations = await layerStatisticsModel.GetActiveRelations(layerData.LayerID, userContext.Transaction);
                    var numRelationChangesHistory = await layerStatisticsModel.GetRelationChangesHistory(layerData.LayerID, userContext.Transaction);
                    var numLayerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layerData.LayerID, userContext.Transaction);
                    var latestChange = await changesetModel.GetLatestChangesetForLayer(layerData.LayerID, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return new LayerStatistics(
                        layerData,
                        numActiveAttributes,
                        numAttributeChangesHistory,
                        numActiveRelations,
                        numRelationChangesHistory,
                        numLayerChangesetsHistory,
                        latestChange?.Timestamp);
                });

            FieldAsync<ListGraphType<OIAContextType>>("manage_oiacontexts",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    CheckManagementPermissionThrow(userContext);

                    var configs = await oiaContextModel.GetContexts(true, userContext.Transaction);

                    return configs;
                });

            FieldAsync<ListGraphType<ODataAPIContextType>>("manage_odataapicontexts",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    CheckManagementPermissionThrow(userContext);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var configs = await odataAPIContextModel.GetAllByCIID(metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return configs.Values;
                });

            FieldAsync<StringGraphType>("manage_baseConfiguration",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    CheckManagementPermissionThrow(userContext);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var cfg = await baseConfigurationModel.GetConfigOrDefault(metaConfiguration.ConfigLayerset, userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                    return BaseConfigurationV2.Serializer.SerializeToString(cfg);
                });

            FieldAsync<ListGraphType<PredicateType>>("manage_predicates",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read predicates");

                    var predicates = await predicateModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return predicates.Values;
                });

            FieldAsync<ListGraphType<RecursiveTraitType>>("manage_recursiveTraits",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read traits");

                    // TODO: should we not deliver non-DB traits (f.e. from CLBs) here?
                    var traitSet = await recursiveDataTraitModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return traitSet.Values;
                });


            FieldAsync<ListGraphType<GeneratorType>>("manage_generators",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read generators");

                    var generators = await generatorModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return generators.Values;
                });

            FieldAsync<ListGraphType<AuthRoleType>>("manage_authRoles",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read auth roles");

                    var authRoles = await authRoleModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return authRoles.Values;
                });

            FieldAsync<ListGraphType<CLConfigType>>("manage_clConfigs",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(TimeThreshold.BuildLatest(), context.Path);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read CL configs");

                    var clConfigs = await clConfigModel.GetAllByDataID(metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return clConfigs.Values;
                });

            FieldAsync<ListGraphType<StringGraphType>>("manage_availablePermissions",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    CheckManagementPermissionThrow(userContext);

                    var allPermissions = await PermissionUtils.GetAllAvailablePermissions(layerModel, userContext.Transaction);
                    return allPermissions;
                });

            FieldAsync<ListGraphType<StringGraphType>>("manage_debugCurrentUser",
                resolve: async context =>
                {
                    var httpContextAccessor = context.RequestServices!.GetRequiredService<IHttpContextAccessor>();
                    var currentAuthenticatedUserService = context.RequestServices!.GetRequiredService<ICurrentUserAccessor>();
                    var claims = httpContextAccessor.HttpContext.User.Claims.Select(c => (c.Type, c.Value));

                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var user = await currentAuthenticatedUserService.GetCurrentUser(modelContextBuilder.BuildImmediate());
                    return claims.Select(kv => $"{kv.Type}: {kv.Value}")
                        .Concat($"Permissions: {string.Join(", ", user.AuthRoles.SelectMany(ar => ar.Permissions).ToHashSet())}")
                        .Concat($"User-Type: {user.InDatabase.UserType}")
                    ;
                });

            Field<VersionType>("manage_version",
                resolve: context =>
                {
                    var loadedPlugins = context.RequestServices!.GetServices<IPluginRegistration>();
                    var coreVersion = VersionService.GetVersion();
                    return new VersionDTO(coreVersion, loadedPlugins);
                });

            Field<ListGraphType<PluginRegistrationType>>("manage_plugins",
                resolve: context =>
                {
                    var plugins = context.RequestServices!.GetServices<IPluginRegistration>();
                    return plugins;
                });
        }
    }
}
