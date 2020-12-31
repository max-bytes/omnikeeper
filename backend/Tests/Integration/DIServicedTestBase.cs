using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Startup;

namespace Tests.Integration
{
    public abstract class DIServicedTestBase : DBBackedTestBase
    {
        private ServiceProvider? serviceProvider;

        private readonly bool enableModelCaching;

        protected DIServicedTestBase(bool enableModelCaching)
        {
            this.enableModelCaching = enableModelCaching;
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            var services = InitServices();
            serviceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            if (serviceProvider != null)
                serviceProvider.Dispose();
        }

        protected ServiceProvider ServiceProvider => serviceProvider!;

        protected virtual IServiceCollection InitServices()
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
    }
}
