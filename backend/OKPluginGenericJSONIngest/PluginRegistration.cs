using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Controllers.Ingest;
using System.Collections.Generic;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginGenericJSONIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override string? ManagementEndpoint { get; } = "manage/plugin/genericJSONIngest";

        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<GenericTraitEntityModel<Context, string>>();
            sc.AddTransient<PassiveFilesController>();
            sc.AddTransient<ManageContextController>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new List<RecursiveTrait>() {
            TraitEntityHelper.Class2RecursiveTrait<Context>(),
        };
    }
}
