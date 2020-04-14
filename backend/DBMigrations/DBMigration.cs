using DbUp;
using DbUp.Engine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.EndsWith(".psql"))
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();
            return result;
        }
    }
}
