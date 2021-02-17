using GraphQL;
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
using Omnikeeper.Utils;
using System;
using System.Linq;
namespace Omnikeeper.GraphQL
{
    public partial class GraphQLQueryRoot
    {
        private void CreateManage()
        {
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
                    var str = RecursiveTraitSet.Serializer.SerializeToString(traitSet);
                    return str;
                });

            Field<ListGraphType<StringGraphType>>("cacheKeys",
                resolve: context =>
                {
                    var memoryCacheModel = context.RequestServices.GetRequiredService<ICacheModel>();

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
                    var loadedPlugins = context.RequestServices.GetServices<IPluginRegistration>();
                    var coreVersion = VersionService.GetVersion();
                    return new VersionDTO(coreVersion, loadedPlugins);
                });

            Field<ListGraphType<PluginRegistrationType>>("plugins",
                resolve: context =>
                {
                    var plugins = context.RequestServices.GetServices<IPluginRegistration>();
                    return plugins;
                });
        }
    }
}
