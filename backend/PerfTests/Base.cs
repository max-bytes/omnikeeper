using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Startup;
using Tests.Integration;

namespace PerfTests
{
    public class Base
    {
        private NpgsqlConnection? conn;
        private ServiceProvider? serviceProvider;

        public virtual void Setup(bool enableModelCaching)
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.Build(DBSetup.dbName, false, true);

            var services = InitServices(enableModelCaching);
            serviceProvider = services.BuildServiceProvider();
        }

        public virtual void TearDown()
        {
            if (conn != null)
                conn.Close();
            if (serviceProvider != null)
                serviceProvider.Dispose();
        }

        protected virtual IServiceCollection InitServices(bool enableModelCaching)
        {
            var services = new ServiceCollection();
            ServiceRegistration.RegisterLogging(services);
            ServiceRegistration.RegisterDB(services, DBSetup.dbName, false, true);
            ServiceRegistration.RegisterOIABase(services);
            ServiceRegistration.RegisterModels(services, enableModelCaching, false, false);
            ServiceRegistration.RegisterServices(services);
            ServiceRegistration.RegisterGraphQL(services);

            if (enableModelCaching)
            {
                services.AddSingleton<IDistributedCache>((sp) =>
                {
                    var opts = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
                    return new MemoryDistributedCache(opts);
                });
            }
            else
            {
                services.AddSingleton<IDistributedCache>((sp) => new Mock<IDistributedCache>().Object);
            }

            // TODO: add generic?
            services.AddSingleton<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance);
            services.AddSingleton<ILogger<BaseConfigurationModel>>((sp) => NullLogger<BaseConfigurationModel>.Instance);
            services.AddSingleton<ILogger<OIAContextModel>>((sp) => NullLogger<OIAContextModel>.Instance);
            services.AddSingleton<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance);
            services.AddSingleton<ILogger<RecursiveTraitModel>>((sp) => NullLogger<RecursiveTraitModel>.Instance);
            services.AddSingleton<ILogger<IModelContext>>((sp) => NullLogger<IModelContext>.Instance);
            services.AddSingleton<ILogger<CachingBaseAttributeModel>>((sp) => NullLogger<CachingBaseAttributeModel>.Instance);
            services.AddSingleton<ILogger<CachingLayerModel>>((sp) => NullLogger<CachingLayerModel>.Instance);
            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

            services.AddSingleton<IConfiguration>((sp) => new Mock<IConfiguration>().Object);

            // override user service
            var currentUserService = new Mock<ICurrentUserService>();
            services.AddSingleton<ICurrentUserService>((sp) => currentUserService.Object);
            services.AddSingleton<ILogger<DataPartitionService>>((sp) => NullLogger<DataPartitionService>.Instance);

            // override authorization
            services.AddSingleton((sp) => new Mock<IManagementAuthorizationService>().Object);
            services.AddSingleton((sp) => new Mock<ILayerBasedAuthorizationService>().Object);
            services.AddSingleton((sp) => new Mock<ICIBasedAuthorizationService>().Object);

            return services;
        }

        protected ServiceProvider ServiceProvider => serviceProvider!;
    }
}
