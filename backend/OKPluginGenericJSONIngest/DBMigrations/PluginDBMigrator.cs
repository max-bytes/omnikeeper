using Omnikeeper.Base.Plugins;
using System.Reflection;

namespace DBMigrations
{
    public class PluginDBMigrator : PluginDBMigratorBase
    {
        public static string PluginDBSchemaName => "generic_json_ingest";
        protected override string _PluginDBSchemaName => PluginDBSchemaName;
        protected override Assembly AssemblyContainingMigrations => typeof(PluginDBMigrator).Assembly;
    }
}
