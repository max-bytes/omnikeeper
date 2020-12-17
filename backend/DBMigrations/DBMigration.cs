using DbUp;
using DbUp.Engine;
using Npgsql;
using System.Linq;
using System.Reflection;

namespace DBMigrations
{
    public class DBMigration
    {
        public static DatabaseUpgradeResult Migrate(string connectionString, bool logOutput)
        {
            EnsureDatabase.For.PostgresqlDatabase(connectionString);

            var builder = DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithTransaction()
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.EndsWith(".psql"));

            if (logOutput) {
                    builder = builder
                        .LogToConsole()
                        .LogScriptOutput();
            } else
            {
                builder = builder.LogToNowhere();
            }

            var upgrader = builder.Build();

            var result = upgrader.PerformUpgrade();
            return result;
        }
    }
}
