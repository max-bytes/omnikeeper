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
            lbas.Setup(x => x.CanUserReadFromAllLayers(It.IsAny<AuthenticatedUser>(), It.IsAny<IEnumerable<string>>())).Returns(true);
            services.AddScoped((sp) => lbas.Object);
            var cbas = new Mock<ICIBasedAuthorizationService>();
            cbas.Setup(x => x.CanReadCI(It.IsAny<Guid>())).Returns(true);
            Guid? tmp;
            cbas.Setup(x => x.CanReadAllCIs(It.IsAny<IEnumerable<Guid>>(), out tmp)).Returns(true);
            cbas.Setup(x => x.FilterReadableCIs(It.IsAny<IEnumerable<Guid>>())).Returns<IEnumerable<Guid>>((i) => i);
            cbas.Setup(x => x.FilterReadableCIs(It.IsAny<IEnumerable<MergedCI>>(), It.IsAny<Func<MergedCI, Guid>>())).Returns<IEnumerable<MergedCI>, Func<MergedCI, Guid>>((i, j) => {
                return i;
            });
            services.AddScoped((sp) => cbas.Object);

            return services;
        }
    }
}
