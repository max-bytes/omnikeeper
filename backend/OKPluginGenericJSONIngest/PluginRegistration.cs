using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Controllers.Ingest;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override string? ManagementEndpoint { get; } = "manage/plugin/genericJSONIngest";

        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<ContextModel>();
            sc.AddSingleton<GenericJsonIngestService>();
            sc.AddSingleton<PassiveFilesController>();
            sc.AddSingleton<ManageContextController>();
            sc.AddSingleton<IIssueContextSource, IssueContextSource>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new List<RecursiveTrait>() {
            GenericTraitEntityHelper.Class2RecursiveTrait<Context>(),
        };
    }
}
