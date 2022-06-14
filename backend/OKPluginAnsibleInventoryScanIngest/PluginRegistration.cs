using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;

namespace OKPluginAnsibleInventoryScanIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddTransient<AnsibleInventoryScanIngestController>();
            sc.AddSingleton<IIssueContextSource, IssueContextSource>();
        }
    }
}
