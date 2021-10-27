using Autofac;
using Autofac.Extensions.DependencyInjection;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Startup;
using System;
using Tests.Integration;

namespace PerfTests
{
    public class Base
    {
        private NpgsqlConnection? conn;
        private AutofacServiceProvider? serviceProvider;

        public virtual void Setup(bool enableModelCaching, bool enableEffectiveTraitCaching, bool setupDBSchema)
        {
            if (setupDBSchema)
            {
                DBSetup.Setup();
            }

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);

            var services = InitServices(enableModelCaching, enableEffectiveTraitCaching);
            serviceProvider = new AutofacServiceProvider(services.Build());
        }

        public virtual void TearDown()
        {
            if (conn != null)
                conn.Close();
            if (serviceProvider != null)
                serviceProvider.Dispose();
        }

        protected virtual ContainerBuilder InitServices(bool enableModelCaching, bool enableEffectiveTraitCaching)
        {
            var containerBuilder = new ContainerBuilder();
            ServiceRegistration.RegisterLogging(containerBuilder);
            ServiceRegistration.RegisterDB(containerBuilder, DBConnectionBuilder.GetConnectionStringFromUserSecrets(GetType().Assembly), true);
            ServiceRegistration.RegisterOIABase(containerBuilder);
            ServiceRegistration.RegisterModels(containerBuilder, enableModelCaching, enableEffectiveTraitCaching, false, false);
            ServiceRegistration.RegisterServices(containerBuilder);
            ServiceRegistration.RegisterGraphQL(containerBuilder);

            if (enableModelCaching)
            {
                containerBuilder.Register<IDistributedCache>((sp) =>
                {
                    var opts = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
                    return new MemoryDistributedCache(opts);
                }).SingleInstance();
            }
            else
            {
                containerBuilder.Register<IDistributedCache>((sp) => new Mock<IDistributedCache>().Object).SingleInstance();
            }

            // TODO: add generic?
            containerBuilder.Register<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<MetaConfigurationModel>>((sp) => NullLogger<MetaConfigurationModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<OIAContextModel>>((sp) => NullLogger<OIAContextModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<IModelContext>>((sp) => NullLogger<IModelContext>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<CachingLayerModel>>((sp) => NullLogger<CachingLayerModel>.Instance).SingleInstance();
            containerBuilder.RegisterType<NullLoggerFactory>().As<ILoggerFactory>().SingleInstance();
            containerBuilder.Register<ILogger<CISearchModel>>((sp) => NullLogger<CISearchModel>.Instance).SingleInstance();

            containerBuilder.Register<IConfiguration>((sp) => new Mock<IConfiguration>().Object).SingleInstance();

            // override user service
            var currentUserService = new Mock<ICurrentUserService>();
            containerBuilder.Register<ICurrentUserService>((sp) => currentUserService.Object).SingleInstance();
            containerBuilder.Register<ILogger<DataPartitionService>>((sp) => NullLogger<DataPartitionService>.Instance).SingleInstance();

            // override authorization
            containerBuilder.Register((sp) => new Mock<IManagementAuthorizationService>().Object).SingleInstance();
            containerBuilder.Register((sp) => new Mock<ILayerBasedAuthorizationService>().Object).SingleInstance();
            containerBuilder.Register((sp) => new Mock<ICIBasedAuthorizationService>().Object).SingleInstance();

            return containerBuilder;
        }

        protected IServiceProvider ServiceProvider => serviceProvider!;
    }
}
