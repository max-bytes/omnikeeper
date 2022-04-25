using Microsoft.Extensions.DependencyInjection;
using OKPluginGenericJSONIngest;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;

namespace OKPluginInsightDiscoveryScanIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IngestFileController>();

            // TODO: find out why we need to add these when the OKPluginGenericJSONIngest plugin is also loaded
            sc.AddSingleton<GenericJsonIngestService>();
            sc.AddSingleton<ContextModel>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new List<RecursiveTrait>() {
            GenericTraitEntityHelper.Class2RecursiveTrait<Context>(),
        };
    }
}
