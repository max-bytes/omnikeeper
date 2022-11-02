using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Startup;
using System;
using Tests.Integration;
using Omnikeeper.Base.Authz;

namespace PerfTests
{
    public class Base
    {
        //private NpgsqlConnection? conn;
        private AutofacServiceProvider? serviceProvider;

        public virtual void Setup(bool enablePerRequestModelCaching, bool setupDBSchema)
        {
            if (setupDBSchema)
            {
                DBSetup.Setup();
            }

            //var dbcb = new DBConnectionBuilder();
            //conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);

            var services = InitServices(enablePerRequestModelCaching);
            serviceProvider = new AutofacServiceProvider(services.Build());
        }

        public virtual void TearDown()
        {
            //if (conn != null)
            //    conn.Close();
            if (serviceProvider != null)
                serviceProvider.Dispose();
        }

        protected virtual ContainerBuilder InitServices(bool enablePerRequestModelCaching)
        {
            var containerBuilder = new ContainerBuilder();
            ServiceRegistration.RegisterLogging(containerBuilder);
            ServiceRegistration.RegisterDB(containerBuilder, DBConnectionBuilder.GetConnectionStringFromUserSecrets(GetType().Assembly), true);
            ServiceRegistration.RegisterOIABase(containerBuilder);
            ServiceRegistration.RegisterModels(containerBuilder, enablePerRequestModelCaching, false, false);
            ServiceRegistration.RegisterServices(containerBuilder);
            ServiceRegistration.RegisterGraphQL(containerBuilder);

            //if (enableModelCaching)
            //{
            //    containerBuilder.Register<IDistributedCache>((sp) =>
            //    {
            //        var opts = Options.Create<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions());
            //        return new MemoryDistributedCache(opts);
            //    }).SingleInstance();
            //}
            //else
            //{
            //    containerBuilder.Register<IDistributedCache>((sp) => new Mock<IDistributedCache>().Object).SingleInstance();
            //}

            // TODO: add generic?
            containerBuilder.Register<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<MetaConfigurationModel>>((sp) => NullLogger<MetaConfigurationModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<OIAContextModel>>((sp) => NullLogger<OIAContextModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<IModelContext>>((sp) => NullLogger<IModelContext>.Instance).SingleInstance();
            containerBuilder.Register<ILogger<CachingLayerModel>>((sp) => NullLogger<CachingLayerModel>.Instance).SingleInstance();
            containerBuilder.RegisterType<NullLoggerFactory>().As<ILoggerFactory>().SingleInstance();

            containerBuilder.Register<IConfiguration>((sp) => new Mock<IConfiguration>().Object).SingleInstance();

            // override user service
            var currentUserService = new Mock<ICurrentUserAccessor>();
            containerBuilder.Register<ICurrentUserAccessor>((sp) => currentUserService.Object).SingleInstance();
            containerBuilder.Register<ILogger<DataPartitionService>>((sp) => NullLogger<DataPartitionService>.Instance).SingleInstance();

            // override authorization
            containerBuilder.Register((sp) => new Mock<IManagementAuthorizationService>().Object).SingleInstance();
            containerBuilder.Register((sp) => new Mock<ILayerBasedAuthorizationService>().Object).SingleInstance();

            return containerBuilder;
        }

        protected IServiceProvider ServiceProvider => serviceProvider!;
    }
}
