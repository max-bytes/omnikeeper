using DBMigrations;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Npgsql;
using System;
using System.Threading.Tasks;
using Omnikeeper.Base.Utils.ModelContext;

namespace Tests.Integration
{
    public class DBSetup
    {
        public static readonly string dbName = "tmp";

        public static void Setup()
        {
            _Setup(dbName);
        }

        private static void _Setup(string _dbName)
        {
            // drop db
            NpgsqlConnection conn = new NpgsqlConnection("Server=localhost;User Id=postgres; Password=postgres;Pooling=false");
            conn.Open();
            // force disconnect other users
            new NpgsqlCommand(@$"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE datname = '{_dbName}' AND pid <> pg_backend_pid();", conn).ExecuteNonQuery();
            new NpgsqlCommand($"DROP DATABASE {_dbName};", conn).ExecuteNonQuery();
            conn.Close();

            // create db, setup schema and migrations
            var migrationResult = DBMigration.Migrate($"Server=localhost;User Id=postgres; Password=postgres;Database={_dbName};Pooling=false");

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
