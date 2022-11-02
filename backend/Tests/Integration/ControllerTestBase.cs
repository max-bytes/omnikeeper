using Autofac;
using Moq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Service;
using System;
using Omnikeeper.Base.Authz;
using System.Collections.Generic;

namespace Tests.Integration.Controller
{
    abstract class ControllerTestBase : DIServicedTestBase
    {
        protected ControllerTestBase() : base(false)
        {
        }

        protected override void InitServices(ContainerBuilder builder)
        {
            base.InitServices(builder);

            // mock authorization
            var lbas = new Mock<ILayerBasedAuthorizationService>();
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<IAuthenticatedUser>(), It.IsAny<Layer>())).Returns(true);
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<IAuthenticatedUser>(), It.IsAny<string>())).Returns(true);
            lbas.Setup(x => x.CanUserReadFromAllLayers(It.IsAny<IAuthenticatedUser>(), It.IsAny<IEnumerable<string>>())).Returns(true);
            builder.Register((sp) => lbas.Object).InstancePerLifetimeScope();

            var mas = new Mock<IManagementAuthorizationService>();
            string outMsg;
            mas.Setup(x => x.CanModifyManagement(It.IsAny<IAuthenticatedUser>(), It.IsAny<MetaConfiguration>(), out outMsg)).Returns(true);
            mas.Setup(x => x.CanReadManagement(It.IsAny<IAuthenticatedUser>(), It.IsAny<MetaConfiguration>(), out outMsg)).Returns(true);
            builder.Register((sp) => mas.Object).InstancePerLifetimeScope();
        }
    }
}
