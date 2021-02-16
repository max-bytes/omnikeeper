using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Plugins;

namespace OKPluginOIAOmnikeeper
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IExternalIDManager, ExternalIDManager>();
            sc.AddSingleton<ILayerAccessProxy, LayerAccessProxy>();
            sc.AddSingleton<IScopedExternalIDMapper, ScopedExternalIDMapper>();
            sc.AddSingleton<IOnlineInboundAdapter, OnlineInboundAdapter>();
            sc.AddSingleton<IOnlineInboundAdapterBuilder, OnlineInboundAdapter.Builder>();
        }
    }
}
