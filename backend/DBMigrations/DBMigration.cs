using DbUp;
using DbUp.Engine;
using System;
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
                    .WithExecutionTimeout(TimeSpan.FromMinutes(180))
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.EndsWith(".psql"));

            if (logOutput)
            {
                builder = builder
                    .LogToConsole()
                    .LogScriptOutput();
            }
            else
            {
                builder = builder.LogToNowhere();
            }

            var upgrader = builder.Build();

            var result = upgrader.PerformUpgrade();
            return result;
        }
    }
}
