﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Config;
using Omnikeeper.Model.Decorators;
using Omnikeeper.Service;
using Omnikeeper.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity.DataOrigin;

namespace Tests.Integration
{
    public abstract class DIServicedTestBase : DBBackedTestBase
    {
        private AutofacServiceProvider? serviceProvider;

        protected bool enablePerRequestModelCaching;
        protected Mock<ICurrentUserAccessor> currentUserServiceMock;

        protected DIServicedTestBase(bool enablePerRequestModelCaching)
        {
            this.enablePerRequestModelCaching = enablePerRequestModelCaching;
            currentUserServiceMock = new Mock<ICurrentUserAccessor>();
        }

        protected DIServicedTestBase() : this(false) { }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            var builder = new ContainerBuilder();
            InitServices(builder);
            var container = builder.Build();
            serviceProvider = new AutofacServiceProvider(container);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            if (serviceProvider != null)
                serviceProvider.Dispose();
        }

        protected async Task<UserInDatabase> SetupDefaultUser()
        {
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            return user;
        }

        protected async Task<ChangesetProxy> CreateChangesetProxy() => CreateChangesetProxy(await SetupDefaultUser(), TimeThreshold.BuildLatest());
        protected async Task<ChangesetProxy> CreateChangesetProxy(TimeThreshold timeThreshold) => CreateChangesetProxy(await SetupDefaultUser(), timeThreshold);
        protected ChangesetProxy CreateChangesetProxy(UserInDatabase user) => CreateChangesetProxy(user, TimeThreshold.BuildLatest());
        protected ChangesetProxy CreateChangesetProxy(UserInDatabase user, TimeThreshold timeThreshold)
        {
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            return new ChangesetProxy(user, timeThreshold, changesetModel, new DataOriginV1(DataOriginType.Manual));
        }

        protected IServiceProvider ServiceProvider => serviceProvider!;

        protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

        protected virtual void InitServices(ContainerBuilder builder)
        {
            ServiceRegistration.RegisterLogging(builder);
            ServiceRegistration.RegisterDB(builder, DBConnectionBuilder.GetConnectionStringFromUserSecrets(GetType().Assembly), true);
            ServiceRegistration.RegisterModels(builder, enablePerRequestModelCaching, false);
            ServiceRegistration.RegisterServices(builder);
            ServiceRegistration.RegisterGraphQL(builder);
            ServiceRegistration.RegisterQuartz(builder, DBConnectionBuilder.GetConnectionStringFromUserSecrets(GetType().Assembly), "instance-A");

            builder.Register<ILogger<EffectiveTraitModel>>((sp) => NullLogger<EffectiveTraitModel>.Instance).SingleInstance();
            builder.Register<ILogger<MetaConfigurationModel>>((sp) => NullLogger<MetaConfigurationModel>.Instance).SingleInstance();
            builder.Register<ILogger<ODataAPIContextModel>>((sp) => NullLogger<ODataAPIContextModel>.Instance).SingleInstance();
            builder.Register<ILogger<IModelContext>>((sp) => NullLogger<IModelContext>.Instance).SingleInstance();
            builder.Register<ILogger<CachingLayerModel>>((sp) => NullLogger<CachingLayerModel>.Instance).SingleInstance();
            builder.Register<ILogger<TraitsProvider>>((sp) => NullLogger<TraitsProvider>.Instance).SingleInstance();
            builder.Register<ILogger<CachingMetaConfigurationModel>>((sp) => NullLogger<CachingMetaConfigurationModel>.Instance).SingleInstance();
            builder.RegisterType<NullLoggerFactory>().As<ILoggerFactory>().SingleInstance();

            builder.Register<IHttpContextAccessor>((sp) => new Mock<IHttpContextAccessor>().Object).SingleInstance();

            builder.Register<IConfiguration>((sp) => new Mock<IConfiguration>().Object).SingleInstance();

            // override user service
            builder.Register<ICurrentUserAccessor>((sp) => currentUserServiceMock.Object).SingleInstance();

            // override authorization
            var mas = new Mock<IManagementAuthorizationService>();
            string? tmpStr;
            mas.Setup(x => x.CanModifyManagement(It.IsAny<IAuthenticatedUser>(), It.IsAny<MetaConfiguration>(), out tmpStr)).Returns(true);
            mas.Setup(x => x.HasManagementPermission(It.IsAny<IAuthenticatedUser>())).Returns(true);
            builder.Register((sp) => mas.Object).SingleInstance();

            var lbas = new Mock<ILayerBasedAuthorizationService>();
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<IAuthenticatedUser>(), It.IsAny<Layer>())).Returns(true);
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<IAuthenticatedUser>(), It.IsAny<string>())).Returns(true);
            lbas.Setup(x => x.CanUserWriteToAllLayers(It.IsAny<IAuthenticatedUser>(), It.IsAny<IEnumerable<string>>())).Returns(true);
            lbas.Setup(e => e.CanUserReadFromAllLayers(It.IsAny<IAuthenticatedUser>(), It.IsAny<IEnumerable<string>>())).Returns(true);
            builder.Register((sp) => lbas.Object).SingleInstance();

            // override quartz schedulers
            var localScheduler = new Mock<Quartz.IScheduler>();
            builder.Register<Quartz.IScheduler>(sp => localScheduler.Object).Keyed<Quartz.IScheduler>("localScheduler").SingleInstance();
            var distributedScheduler = new Mock<Quartz.IScheduler>();
            builder.Register<Quartz.IScheduler>(sp => distributedScheduler.Object).Keyed<Quartz.IScheduler>("distributedScheduler").SingleInstance();
        }
    }
}
