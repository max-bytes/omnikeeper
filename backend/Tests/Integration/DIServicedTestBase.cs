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

namespace Tests.Integration
{
    abstract class DIServicedTestBase
    {
        [SetUp]
        public void Setup()
        {
            var services = InitServices();
            ServiceProvider = services.BuildServiceProvider();
            DBSetup.Setup();
        }


        [TearDown]
        public void TearDown()
        {
            ServiceProvider.Dispose();
        }

        protected ServiceProvider ServiceProvider { get; private set; }

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

            services.AddScoped<IMemoryCacheModel>((sp) => null);

            // TODO: add generic?
            services.AddScoped<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance);
            services.AddScoped<ILogger<BaseConfigurationModel>>((sp) => NullLogger<BaseConfigurationModel>.Instance);
            services.AddScoped<ILogger<OIAContextModel>>((sp) => NullLogger<OIAContextModel>.Instance);
            services.AddScoped<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance);
            services.AddScoped<ILogger<RecursiveTraitModel>>((sp) => NullLogger<RecursiveTraitModel>.Instance);

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
