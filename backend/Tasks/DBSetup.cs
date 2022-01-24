using DBMigrations;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Tasks
{
    class DBSetup
    {
        public static void Setup()
        {
            var connectionString = DBConnectionBuilder.GetConnectionStringFromUserSecrets(typeof(DBSetup).Assembly);
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            connectionStringBuilder.Pooling = false;
            var dbName = connectionStringBuilder.Database;
            connectionStringBuilder.Database = null; // set database to null for the first connection to be able to drop it
            // drop db
            NpgsqlConnection conn = new NpgsqlConnection(connectionStringBuilder.ToString());
            conn.Open();
            // force disconnect other users
            new NpgsqlCommand(@$"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE datname = '{dbName}' AND pid <> pg_backend_pid();", conn).ExecuteNonQuery();
            new NpgsqlCommand($"DROP DATABASE {dbName};", conn).ExecuteNonQuery();
            conn.Close();

            // create db, setup schema and migrations
            connectionStringBuilder.Database = dbName;
            var migrationResult = DBMigration.Migrate(connectionStringBuilder.ToString(), true);

            if (!migrationResult.Successful)
                throw new Exception("Database migration failed!", migrationResult.Error);
        }

        public static async Task<UserInDatabase> SetupUser(IUserInDatabaseModel userModel, IModelContext trans, string username = "test-user", Guid? userGUID = null, UserType type = UserType.Robot)
        {
            var guid = userGUID ?? new Guid("2544f9a7-cc17-4cba-8052-f88656cf1ef1");
            return await userModel.UpsertUser(username, username, guid, type, trans);
        }
    }
}
