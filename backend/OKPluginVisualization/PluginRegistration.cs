using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Plugins;

namespace OKPluginVisualization
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<GraphvizDotController>();
        }
    }
}
