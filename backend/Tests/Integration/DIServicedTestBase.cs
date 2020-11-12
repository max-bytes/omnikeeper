using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Utils;
using Moq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Startup;
using Microsoft.Extensions.Logging;
using Omnikeeper.Model.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Omnikeeper.Service;
using FluentAssertions;
using Omnikeeper.Base.Entity.DTO;
using Microsoft.AspNetCore.Mvc;
using Omnikeeper.Base.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration
{
    abstract class DIServicedTestBase : DBBackedTestBase
    {
        private ServiceProvider? serviceProvider;

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
            ServiceRegistration.RegisterOKPlugins(services);
            ServiceRegistration.RegisterModels(services, false, false);
            ServiceRegistration.RegisterServices(services);
            ServiceRegistration.RegisterGraphQL(services);

            services.AddScoped<IMemoryCache>((sp) => new Mock<IMemoryCache>().Object);

            // TODO: add generic?
            services.AddScoped<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance);
            services.AddScoped<ILogger<BaseConfigurationModel>>((sp) => NullLogger<BaseConfigurationModel>.Instance);
            services.AddScoped<ILogger<OIAContextModel>>((sp) => NullLogger<OIAContextModel>.Instance);
            services.AddScoped<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance);
            services.AddScoped<ILogger<RecursiveTraitModel>>((sp) => NullLogger<RecursiveTraitModel>.Instance);
            services.AddScoped<ILogger<IModelContext>>((sp) => NullLogger<IModelContext>.Instance);
            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

            services.AddSingleton<IConfiguration>((sp) => new Mock<IConfiguration>().Object);

            // override user service
            var currentUserService = new Mock<ICurrentUserService>();
            services.AddScoped<ICurrentUserService>((sp) => currentUserService.Object);

            // override authorization
            services.AddScoped((sp) => new Mock<IManagementAuthorizationService>().Object);
            services.AddScoped((sp) => new Mock<ILayerBasedAuthorizationService>().Object);
            services.AddScoped((sp) => new Mock<ICIBasedAuthorizationService>().Object);

            return services;
        }
    }
}
