using GraphQL;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using Omnikeeper.Ingest.ActiveDirectoryXML;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Utils;

namespace Omnikeeper.Startup
{
    public static class ServiceRegistration
    {
        public static void RegisterDB(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(configuration);
            });
            services.AddScoped<IModelContextBuilder, ModelContextBuilder>();
        }
        public static void RegisterDB(IServiceCollection services, string dbName, bool pooling, bool reloadTypes)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(dbName, pooling, reloadTypes);
            });
            services.AddScoped<IModelContextBuilder, ModelContextBuilder>();
        }

        public static void RegisterOIABase(IServiceCollection services)
        {
            services.AddSingleton<IOnlineAccessProxy, OnlineAccessProxy>();
            services.AddSingleton<IExternalIDMapper, ExternalIDMapper>();
            services.AddSingleton<IExternalIDMapPersister, ExternalIDMapPostgresPersister>();
            services.AddSingleton<IInboundAdapterManager, InboundAdapterManager>();
        }

        public static void RegisterOKPlugins(IServiceCollection services)
        {
            // register compute layer brains
            services.AddSingleton<IComputeLayerBrain, OKPluginCLBMonitoring.CLBNaemonMonitoring>();

            // register online inbound adapters
            services.AddSingleton<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.Builder>();
            services.AddSingleton<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.BuilderInternal>();
            services.AddSingleton<IOnlineInboundAdapterBuilder, OKPluginOIAOmnikeeper.OnlineInboundAdapter.Builder>();
            services.AddSingleton<IOnlineInboundAdapterBuilder, OKPluginOIASharepoint.OnlineInboundAdapter.Builder>();

            // register ingest adapters
            services.AddSingleton<ActiveDirectoryXMLIngestService, ActiveDirectoryXMLIngestService>();
        }

        public static void RegisterServices(IServiceCollection services)
        {
            // TODO: make singleton
            services.AddSingleton<CIMappingService, CIMappingService>();
            services.AddSingleton<IManagementAuthorizationService, ManagementAuthorizationService>();
            services.AddSingleton<ILayerBasedAuthorizationService, LayerBasedAuthorizationService>();
            services.AddSingleton<ICIBasedAuthorizationService, CIBasedAuthorizationService>();
            services.AddSingleton<MarkedForDeletionService>();
            services.AddScoped<IngestDataService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddSingleton<ReactiveLogReceiver>();
        }

        public static void RegisterLogging(IServiceCollection services)
        {
            services.AddSingleton<NpgsqlLoggingProvider>();
        }

        public static void RegisterModels(IServiceCollection services, bool enableModelCaching, bool enableOIA)
        {
            services.AddSingleton<ICISearchModel, CISearchModel>();
            services.AddSingleton<ICIModel, CIModel>();
            services.AddSingleton<IAttributeModel, AttributeModel>();
            services.AddSingleton<IBaseAttributeModel, BaseAttributeModel>();
            services.AddSingleton<IUserInDatabaseModel, UserInDatabaseModel>();
            services.AddSingleton<ILayerModel, LayerModel>();
            services.AddSingleton<ILayerStatisticsModel, LayerStatisticsModel>();
            services.AddSingleton<IRelationModel, RelationModel>();
            services.AddSingleton<IBaseRelationModel, BaseRelationModel>();
            services.AddSingleton<IChangesetModel, ChangesetModel>();
            services.AddSingleton<ITemplateModel, TemplateModel>();
            services.AddSingleton<IPredicateModel, PredicateModel>();
            services.AddSingleton<IMemoryCacheModel, MemoryCacheModel>();
            services.AddSingleton<IODataAPIContextModel, ODataAPIContextModel>();
            services.AddSingleton<IRecursiveTraitModel, RecursiveTraitModel>();
            services.AddSingleton<IEffectiveTraitModel, EffectiveTraitModel>();
            services.AddSingleton<IBaseConfigurationModel, BaseConfigurationModel>();
            services.AddSingleton<IOIAContextModel, OIAContextModel>();
            services.AddSingleton<IGridViewConfigModel, GridViewConfigModel>();

            // these aren't real models, but we keep them here because they are closely related to models
            services.AddSingleton<ITraitsProvider, TraitsProvider>();
            services.AddSingleton<ITemplatesProvider, TemplatesProvider>();

            if (enableModelCaching)
            {
                services.Decorate<IBaseAttributeModel, CachingBaseAttributeModel>();
                services.Decorate<ILayerModel, CachingLayerModel>();
                services.Decorate<IBaseRelationModel, CachingBaseRelationModel>();
                services.Decorate<IPredicateModel, CachingPredicateModel>();
                services.Decorate<IODataAPIContextModel, CachingODataAPIContextModel>();
                services.Decorate<IRecursiveTraitModel, CachingRecursiveTraitModel>();
                services.Decorate<IBaseConfigurationModel, CachingBaseConfigurationModel>();

                services.Decorate<ITemplatesProvider, CachedTemplatesProvider>();
            }

            if (enableOIA)
            {
                services.Decorate<IBaseAttributeModel, OIABaseAttributeModel>();
                services.Decorate<IBaseRelationModel, OIABaseRelationModel>();
            }
        }

        public static void RegisterGraphQL(IServiceCollection services)
        {
            services.AddSingleton<ISchema, GraphQLSchema>();
            services.AddSingleton<IDocumentExecuter, MyDocumentExecutor>(); // custom document executor that does serial queries, required by postgres
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
        }
    }
}
