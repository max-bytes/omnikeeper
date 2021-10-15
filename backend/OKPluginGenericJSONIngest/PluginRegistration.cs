using DBMigrations;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Controllers.Ingest;
using System;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override IPluginDBMigrator? DBMigration => new PluginDBMigrator();

        public override string? ManagementEndpoint { get; } = "manage/plugin/genericJSONIngest";

        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<GenericTraitEntityModel<Context, string>>();
            sc.AddTransient<PassiveFilesController>();
            sc.AddTransient<ManageContextController>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new List<RecursiveTrait>() {
            TraitBuilderFromClass.Class2RecursiveTrait<Context>(),
        };
    }
}
