using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Plugins;
using static OKPluginOIAKeycloak.OnlineInboundAdapter;

namespace OKPluginOIAKeycloak
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IExternalIDManager, KeycloakExternalIDManager>();
            sc.AddSingleton<ILayerAccessProxy, KeycloakLayerAccessProxy>();
            sc.AddSingleton<IScopedExternalIDMapper, KeycloakScopedExternalIDMapper>();
            sc.AddSingleton<IOnlineInboundAdapter, OnlineInboundAdapter>();
            sc.AddSingleton<IOnlineInboundAdapterBuilder, BuilderInternal>();
            sc.AddSingleton<IOnlineInboundAdapterBuilder, Builder>();
        }
    }
}
