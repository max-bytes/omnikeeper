using Microsoft.Extensions.DependencyInjection;
using Moq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;

namespace Tests.Integration.Controller
{
    abstract class ControllerTestBase : DIServicedTestBase
    {
        protected ControllerTestBase() : base(false)
        {
        }

        protected override IServiceCollection InitServices()
        {
            var services = base.InitServices();

            // mock authorization
            var lbas = new Mock<ILayerBasedAuthorizationService>();
            lbas.Setup(x => x.CanUserWriteToLayer(It.IsAny<AuthenticatedUser>(), It.IsAny<Layer>())).Returns(true);
            services.AddScoped((sp) => lbas.Object);
            var cbas = new Mock<ICIBasedAuthorizationService>();
            cbas.Setup(x => x.CanReadCI(It.IsAny<Guid>())).Returns(true);
            Guid? tmp;
            cbas.Setup(x => x.CanReadAllCIs(It.IsAny<IEnumerable<Guid>>(), out tmp)).Returns(true);
            services.AddScoped((sp) => cbas.Object);

            return services;
        }
    }
}
