using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using Omnikeeper.Ingest.ActiveDirectoryXML;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;

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
        }
        public static void RegisterDB(IServiceCollection services, string dbName, bool pooling, bool reloadTypes)
        {
            services.AddSingleton<DBConnectionBuilder>();
            services.AddScoped((sp) =>
            {
                var dbcb = sp.GetRequiredService<DBConnectionBuilder>();
                return dbcb.Build(dbName, pooling, reloadTypes);
            });
        }

        public static void RegisterOIABase(IServiceCollection services)
        {
            services.AddScoped<IOnlineAccessProxy, OnlineAccessProxy>();
            services.AddSingleton<IExternalIDMapper, ExternalIDMapper>();
            services.AddSingleton<IExternalIDMapPersister, ExternalIDMapPostgresPersister>();
            services.AddScoped<IInboundAdapterManager, InboundAdapterManager>();
        }

        public static void RegisterOKPlugins(IServiceCollection services)
        {
            // register compute layer brains
            services.AddScoped<IComputeLayerBrain, OKPluginCLBMonitoring.CLBNaemonMonitoring>();

            // register online inbound adapters
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.Builder>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAKeycloak.OnlineInboundAdapter.BuilderInternal>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIAOmnikeeper.OnlineInboundAdapter.Builder>();
            services.AddScoped<IOnlineInboundAdapterBuilder, OKPluginOIASharepoint.OnlineInboundAdapter.Builder>();
        }

        public static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<CIMappingService, CIMappingService>();
            services.AddScoped<IManagementAuthorizationService, ManagementAuthorizationService>();
            services.AddScoped<ILayerBasedAuthorizationService, LayerBasedAuthorizationService>();
            services.AddScoped<ICIBasedAuthorizationService, CIBasedAuthorizationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<MarkedForDeletionService>();
            services.AddScoped<IngestDataService>();
            services.AddScoped<IngestActiveDirectoryXMLService, IngestActiveDirectoryXMLService>(); // TODO: move to its own plugin

        }

        public static void RegisterLogging(IServiceCollection services)
        {
            services.AddSingleton<NpgsqlLoggingProvider>();
        }

        public static void RegisterModels(IServiceCollection services, bool enableModelCaching, bool enableOIA)
        {
            services.AddScoped<ICISearchModel, CISearchModel>();
            services.AddScoped<ICIModel, CIModel>();
            services.AddScoped<IAttributeModel, AttributeModel>();
            services.AddScoped<IBaseAttributeModel, BaseAttributeModel>();
            services.AddScoped<IUserInDatabaseModel, UserInDatabaseModel>();
            services.AddScoped<ILayerModel, LayerModel>();
            services.AddScoped<ILayerStatisticsModel, LayerStatisticsModel>();
            services.AddScoped<IRelationModel, RelationModel>();
            services.AddScoped<IBaseRelationModel, BaseRelationModel>();
            services.AddScoped<IChangesetModel, ChangesetModel>();
            services.AddScoped<ITemplateModel, TemplateModel>();
            services.AddScoped<IPredicateModel, PredicateModel>();
            services.AddScoped<IMemoryCacheModel, MemoryCacheModel>();
            services.AddScoped<IODataAPIContextModel, ODataAPIContextModel>();
            services.AddScoped<IRecursiveTraitModel, RecursiveTraitModel>();
            services.AddScoped<IEffectiveTraitModel, EffectiveTraitModel>();
            services.AddScoped<IBaseConfigurationModel, BaseConfigurationModel>();
            services.AddScoped<IOIAContextModel, OIAContextModel>();

            // these aren't real models, but we keep them here because they are closely related to models
            services.AddScoped<ITraitsProvider, TraitsProvider>();
            services.AddScoped<ITemplatesProvider, TemplatesProvider>();

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
            services.AddScoped<ISchema, GraphQLSchema>();
            services.AddSingleton<IDocumentExecuter, MyDocumentExecutor>(); // custom document executor that does serial queries, required by postgres
        }
    }
}
