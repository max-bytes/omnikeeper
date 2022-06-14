using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Controllers.Ingest;
using Omnikeeper.Ingest.ActiveDirectoryXML;

namespace OKPluginActiveDirectoryXMLIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<ActiveDirectoryXMLIngestService>();
            sc.AddTransient<ActiveDirectoryXMLIngestController>();
            sc.AddSingleton<IIssueContextSource, IssueContextSource>();
        }
    }
}
