using DBMigrations;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Controllers;
using LandscapeRegistry.Entity;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Integration
{
    class DBSetup
    {
        public static readonly string dbName = "tmp";

        public static void Setup()
        {
            _Setup(dbName);
        }   

        public static void _Setup(string _dbName)
        {
            // drop db
            NpgsqlConnection conn = new NpgsqlConnection("Server=localhost;User Id=postgres; Password=postgres;Pooling=false");
            conn.Open();
            new NpgsqlCommand($"DROP DATABASE {_dbName};", conn).ExecuteNonQuery();
            conn.Close();

            // create db, setup schema and migrations
            var migrationResult = DBMigration.Migrate($"Server=localhost;User Id=postgres; Password=postgres;Database={_dbName};Pooling=false");

            if (!migrationResult.Successful)
                throw new Exception("Database migration failed!", migrationResult.Error);
        }

        public static async Task<UserInDatabase> SetupUser(IUserInDatabaseModel userModel, string username = "test-user", Guid? userGUID = null, UserType type = UserType.Robot)
        {
            var guid = userGUID ?? new Guid("2544f9a7-cc17-4cba-8052-f88656cf1ef1");
            return await userModel.UpsertUser(username, guid, type, null);
        }
    }
}
