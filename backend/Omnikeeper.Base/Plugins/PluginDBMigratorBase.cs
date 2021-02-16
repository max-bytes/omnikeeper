using DbUp;
using DbUp.Engine;
using DbUp.Helpers;
using System.Reflection;

namespace Omnikeeper.Base.Plugins
{
    public interface IPluginDBMigrator
    {
        public DatabaseUpgradeResult Migrate(string connectionString);
    }
    public abstract class PluginDBMigratorBase : IPluginDBMigrator
    {
        protected abstract string _PluginDBSchemaName { get; }
        protected abstract Assembly AssemblyContainingMigrations { get; }
        public DatabaseUpgradeResult Migrate(string connectionString)
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            // schema builder
            // NOTE: workaround because of https://github.com/DbUp/DbUp/issues/346
            var schemaBuilder = DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .LogToNowhere()
                    .WithTransaction()
                    .JournalTo(new NullJournal())
                    .WithScript(new SqlScript("00000-create-schema.psql", $@"
                    CREATE SCHEMA IF NOT EXISTS {_PluginDBSchemaName};
                    "));
            var schemaUpgrader = schemaBuilder.Build();
            var schemaResult = schemaUpgrader.PerformUpgrade();
            if (!schemaResult.Successful)
                return schemaResult;

            // perform actual migrations
            var builder = DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .JournalToPostgresqlTable(_PluginDBSchemaName, "SchemaVersions")
                    .WithTransaction()
                    .WithVariable("SchemaName", _PluginDBSchemaName)
                    .WithScriptsEmbeddedInAssembly(AssemblyContainingMigrations, s => s.EndsWith(".psql"))
                    .LogToConsole()
                    .LogScriptOutput();

            var upgrader = builder.Build();

            var result = upgrader.PerformUpgrade();
            return result;
        }
    }
}
