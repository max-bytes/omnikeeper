using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL.Types;
using Omnikeeper.Service;
using Quartz.Impl;
using System;
using System.Collections.Generic;
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
            Field<ListGraphType<LayerDataType>>("manage_layers")
                .Resolve(context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext);

                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(layersDict => layersDict.Values);
                });

            Field<LayerStatisticsType>("manage_layerStatistics")
                .Arguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "layerID" })
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

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
                    var latestChange = await changesetModel.GetLatestChangesetOverall(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, PredicateSelectionAll.Instance, new string[] { layerData.LayerID }, userContext.Transaction, userContext.GetTimeThreshold(context.Path));

                    return new LayerStatistics(
                        layerData,
                        numActiveAttributes,
                        numAttributeChangesHistory,
                        numActiveRelations,
                        numRelationChangesHistory,
                        numLayerChangesetsHistory,
                        latestChange?.Timestamp);
                });

            Field<ListGraphType<OIAContextType>>("manage_oiacontexts")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext);

                    var configs = await oiaContextModel.GetContexts(true, userContext.Transaction);

                    return configs;
                });

            Field<StringGraphType>("manage_baseConfiguration")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext);

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);

                    var cfg = await baseConfigurationModel.GetConfigOrDefault(metaConfiguration.ConfigLayerset, userContext.GetTimeThreshold(context.Path), userContext.Transaction);
                    return BaseConfigurationV2.Serializer.SerializeToString(cfg);
                });

            Field<ListGraphType<RecursiveTraitType>>("manage_recursiveTraits")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read traits");

                    // TODO: should we not deliver non-DB traits (f.e. from CLBs) here?
                    var traitSet = await recursiveDataTraitModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return traitSet.Values;
                });


            Field<ListGraphType<GeneratorType>>("manage_generators")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read generators");

                    var generators = await generatorModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return generators.Values;
                });

            Field<ListGraphType<AuthRoleType>>("manage_authRoles")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read auth roles");

                    var authRoles = await authRoleModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return authRoles.Values;
                });

            Field<ListGraphType<CLConfigType>>("manage_clConfigs")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read CL configs");

                    var clConfigs = await clConfigModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return clConfigs.Values;
                });


            Field<ListGraphType<ValidatorContextType>>("manage_validatorContexts")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                    CheckReadManagementThrow(userContext, metaConfiguration, "read validator contexts configs");

                    var contexts = await validatorContextModel.GetByDataID(AllCIIDsSelection.Instance, metaConfiguration.ConfigLayerset, userContext.Transaction, TimeThreshold.BuildLatest());
                    return contexts.Values;
                });

            Field<ListGraphType<StringGraphType>>("manage_availablePermissions")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext);

                    var allPermissions = await PermissionUtils.GetAllAvailablePermissions(layerModel, userContext.Transaction);
                    return allPermissions;
                });

            Field<ListGraphType<StringGraphType>>("manage_debugCurrentUser")
                .Resolve(context =>
                {
                    var userContext = context.GetUserContext();
                    if (userContext.User is not AuthenticatedHttpUser ahu)
                        throw new Exception("Invalid user");

                    return ahu.HttpUser.Claims.Select(kv => $"{kv.Type}: {kv.Value}")
                        .Concat($"Permissions: {string.Join(", ", ahu.AuthRoles.SelectMany(ar => ar.Permissions).ToHashSet())}")
                        .Concat($"User-Type: {ahu.InDatabase.UserType}")
                    ;
                });

            Field<VersionType>("manage_version")
                .Resolve(context =>
                {
                    var loadedPlugins = context.RequestServices!.GetServices<IPluginRegistration>();
                    var coreVersion = VersionService.GetVersion();
                    return new VersionDTO(coreVersion, loadedPlugins);
                });

            Field<ListGraphType<RunningJobType>>("manage_runningJobs")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext);

                    var localJobs = await localScheduler.GetCurrentlyExecutingJobs();
                    var distributedJobs = await distributedScheduler.GetCurrentlyExecutingJobs();
                    var ret = new List<RunningJob>();
                    foreach (var job in localJobs.Union(distributedJobs))
                    {
                        var startedAt = job.FireTimeUtc;
                        var runningFor = DateTimeOffset.UtcNow.Subtract(startedAt);
                        var name = "unknown";
                        if(job.JobDetail is JobDetailImpl jdi)
                            name = jdi.FullName;
                        ret.Add(new RunningJob(name, startedAt, runningFor));
                    }
                    //ret.Add(new RunningJob("Dummy", DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(1)), TimeSpan.FromMilliseconds(5234)));
                    return ret;
                });

            Field<ListGraphType<PluginRegistrationType>>("manage_plugins")
                .Resolve(context =>
                {
                    var plugins = context.RequestServices!.GetServices<IPluginRegistration>();
                    return plugins;
                });

            Field<ListGraphType<UserInDatabaseType>>("manage_users")
                .ResolveAsync(async context =>
                {
                    var userContext = context.GetUserContext();

                    CheckManagementPermissionThrow(userContext, "read users");

                    var users = await userInDatabaseModel.GetUsers(null, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    return users;
                });
        }
    }
}
