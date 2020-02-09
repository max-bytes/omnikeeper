using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Tests.Integration
{
    class CIModelTest
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

        private readonly string dbName = "tmp";

        [SetUp]
        public void Setup()
        {
            // re-create db
            NpgsqlConnection conn = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres; Password=postgres;Pooling=false");
            conn.Open();
            var sqlCreateDB = LoadFile("createDB.sql", dbName);
            new NpgsqlCommand(sqlCreateDB, conn).ExecuteNonQuery();
            conn.Close();

            // setup schema
            NpgsqlConnection conn2 = new NpgsqlConnection($"Server=127.0.0.1;User Id=postgres; Password=postgres;Database={dbName};Pooling=false");
            conn2.Open();
            var sqlCreateSchema = LoadFile("createSchema.sql", dbName);
            new NpgsqlCommand(sqlCreateSchema, conn2).ExecuteNonQuery();
            conn2.Close();
        }

        [Test]
        public void TestAddingUpdatingRemovingAndRenewingOfAttributes()
        {
            var model = new CIModel();

            using (var conn = model.CreateOpenConnection(dbName))
            {
                var ciid1 = model.CreateCI("H123", conn);
                Assert.AreEqual(1, ciid1);
                Assert.Throws<PostgresException>(() => model.CreateCI("H123", conn)); // cannot add same identity twice

                var layerID1 = model.CreateLayer("l1", conn);
                Assert.AreEqual(1, layerID1);
                Assert.Throws<PostgresException>(() => model.CreateLayer("l1", conn)); // cannot add same layer twice

                Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text1"), layerID1, "H123", conn));
                Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text2"), layerID1, "H123", conn));

                var a1 = model.GetMergedAttributes("H123", false, conn);
                Assert.AreEqual(1, a1.Count());
                var aa1 = a1.First();
                Assert.AreEqual(ciid1, aa1.CIID);
                Assert.AreEqual(layerID1, aa1.LayerID);
                Assert.AreEqual("a1", aa1.Name);
                Assert.AreEqual(AttributeState.Changed, aa1.State);
                Assert.AreEqual(AttributeValueText.Build("text2"), aa1.Value);

                Assert.IsTrue(model.RemoveAttribute("a1", layerID1, "H123", conn));

                var a2 = model.GetMergedAttributes("H123", false, conn);
                Assert.AreEqual(0, a2.Count());
                var a3 = model.GetMergedAttributes("H123", true, conn);
                Assert.AreEqual(1, a3.Count());
                var aa3 = a3.First();
                Assert.AreEqual(AttributeState.Removed, aa3.State);

                Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text3"), layerID1, "H123", conn));

                var a4 = model.GetMergedAttributes("H123", false, conn);
                Assert.AreEqual(1, a4.Count());
                var aa4 = a4.First();
                Assert.AreEqual(AttributeState.Renewed, aa4.State);
                Assert.AreEqual(AttributeValueText.Build("text3"), aa4.Value);
            }

        }
    }
}
