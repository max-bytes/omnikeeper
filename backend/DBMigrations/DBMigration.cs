using DbUp;
using DbUp.Engine;
using Npgsql;
using System.Linq;
using System.Reflection;

namespace DBMigrations
{
    public class DBMigration
    {
        public static DatabaseUpgradeResult Migrate(string connectionString)
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            var upgrader =
                DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithTransaction()
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.EndsWith(".psql"))
                    .LogToConsole()
                    .LogScriptOutput()
                    .Build();

            var result = upgrader.PerformUpgrade();
            return result;
        }
    }
}
