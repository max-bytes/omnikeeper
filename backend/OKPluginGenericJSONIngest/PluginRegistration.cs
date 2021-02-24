using DBMigrations;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Controllers.Ingest;
using System;

namespace OKPluginGenericJSONIngest
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override IPluginDBMigrator? DBMigration => new PluginDBMigrator();

        public override string? ManagementEndpoint { get; } = "manage/plugin/genericJSONIngest";

        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IContextModel, ContextModel>();
            sc.AddTransient<PassiveFilesController>();
        }
    }
}
