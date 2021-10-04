﻿using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
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

        private void CheckReadManagementThrow(OmnikeeperUserContext userContext, BaseConfigurationV1 baseConfiguration, string reasonForCheck)
        {
            if (!managementAuthorizationService.CanReadManagement(userContext.User, baseConfiguration, out var message))
                throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to {reasonForCheck}: {message}");
        }

        private void CreateManage()
        {
            FieldAsync<ListGraphType<LayerType>>("manage_layers",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    CheckManagementPermissionThrow(userContext);

                    var layers = await layerModel.GetLayers(userContext.Transaction);

                    return layers;
                });

            FieldAsync<LayerStatisticsType>("manage_layerStatistics",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "layerID" }),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    var layerID = context.GetArgument<string>("layerID")!;

                    var layer = await layerModel.GetLayer(layerID, userContext.Transaction);
                    if (layer == null)
                        throw new Exception($"Could not get layer with ID {layerID}");

                    CheckManagementPermissionThrow(userContext);

                    var numActiveAttributes = await layerStatisticsModel.GetActiveAttributes(layer.ID, userContext.Transaction);
                    var numAttributeChangesHistory = await layerStatisticsModel.GetAttributeChangesHistory(layer.ID, userContext.Transaction);
                    var numActiveRelations = await layerStatisticsModel.GetActiveRelations(layer.ID, userContext.Transaction);
                    var numRelationChangesHistory = await layerStatisticsModel.GetRelationChangesHistory(layer.ID, userContext.Transaction);
                    var numLayerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layer.ID, userContext.Transaction);

                    return new LayerStatistics(
                        layer,
                        numActiveAttributes,
                        numAttributeChangesHistory,
                        numActiveRelations,
                        numRelationChangesHistory,
                        numLayerChangesetsHistory);
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
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    CheckManagementPermissionThrow(userContext);

                    var configs = await odataAPIContextModel.GetContexts(userContext.Transaction);

                    return configs;
                });

            FieldAsync<StringGraphType>("manage_baseConfiguration",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate());

                    CheckManagementPermissionThrow(userContext);

                    var cfg = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    return BaseConfigurationV1.Serializer.SerializeToString(cfg);
                });

            FieldAsync<ListGraphType<PredicateType>>("manage_predicates",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, baseConfiguration, "read predicates");

                    var predicates = (await predicateModel.GetPredicates(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, userContext.TimeThreshold)).Values;

                    return predicates;
                });

            FieldAsync<ListGraphType<RecursiveTraitType>>("manage_recursiveTraits",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, baseConfiguration, "read traits");

                    // TODO: should we not deliver non-DB traits (f.e. from CLBs) here?
                    var traitSet = await recursiveDataTraitModel.GetRecursiveTraits(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, TimeThreshold.BuildLatest());
                    return traitSet;
                });


            FieldAsync<ListGraphType<GeneratorType>>("manage_generators",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, baseConfiguration, "read generators");

                    var generators = await generatorModel.GetGenerators(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, TimeThreshold.BuildLatest());
                    return generators.Values;
                });

            FieldAsync<ListGraphType<AuthRoleType>>("manage_authRoles",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, baseConfiguration, "read auth roles");

                    var authRoles = await authRoleModel.GetAuthRoles(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, TimeThreshold.BuildLatest());
                    return authRoles.Values;
                });

            FieldAsync<ListGraphType<CLConfigType>>("manage_clConfigs",
                resolve: async context =>
                {
                    var userContext = context.SetupUserContext()
                        .WithTransaction(modelContextBuilder => modelContextBuilder.BuildImmediate())
                        .WithTimeThreshold(() => TimeThreshold.BuildLatest());

                    var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, baseConfiguration, "read CL configs");

                    var clConfigs = await clConfigModel.GetCLConfigs(new LayerSet(baseConfiguration.ConfigLayerset), userContext.Transaction, TimeThreshold.BuildLatest());
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

            Field<ListGraphType<StringGraphType>>("manage_cacheKeys",
                resolve: context =>
                {
                    var memoryCacheModel = context.RequestServices!.GetRequiredService<ICacheModel>();

                    var keys = memoryCacheModel.GetKeys();
                    return keys;
                });

            FieldAsync<ListGraphType<StringGraphType>>("manage_debugCurrentUser",
                resolve: async context =>
                {
                    var currentUserService = context.RequestServices!.GetRequiredService<ICurrentUserService>();
                    var claims = currentUserService.DebugGetAllClaims();

                    var modelContextBuilder = context.RequestServices!.GetRequiredService<IModelContextBuilder>();
                    var user = await currentUserService.GetCurrentUser(modelContextBuilder.BuildImmediate());
                    return claims.Select(kv => $"{kv.type}: {kv.value}")
                        .Concat($"Permissions: {string.Join(", ", user.Permissions)}")
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
