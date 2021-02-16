using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Controllers.Ingest;

namespace OKPluginAnsibleInventoryScanIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddTransient<AnsibleInventoryScanIngestController>();
        }
    }
}
