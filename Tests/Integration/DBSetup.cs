using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tests.Integration
{
    class DBSetup
    {
        private static string GetFilename(string testDataFolder)
        {
            string startupPath = ApplicationEnvironment.ApplicationBasePath;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            string projectPath = String.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, "DBSchema", testDataFolder);
        }

        private static string LoadFile(string filename, string dbName)
        {
            var path = GetFilename(filename);
            string strText = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            strText = strText.Replace(":databaseName", dbName);
            return strText;
        }

        public static readonly string dbName = "tmp";

        public static void Setup()
        {
            _Setup(dbName);
        }

        public static void _Setup(string _dbName)
        {
            // re-create db
            NpgsqlConnection conn = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres; Password=postgres;Pooling=false");
            conn.Open();
            var sqlCreateDB = LoadFile("createDB.sql", _dbName);
            new NpgsqlCommand(sqlCreateDB, conn).ExecuteNonQuery();
            conn.Close();

            // setup schema
            NpgsqlConnection conn2 = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={_dbName};Pooling=false");
            conn2.Open();
            var sqlCreateSchema = LoadFile("createSchema.sql", _dbName);
            new NpgsqlCommand(sqlCreateSchema, conn2).ExecuteNonQuery();
            conn2.Close();
        }
    }
}
