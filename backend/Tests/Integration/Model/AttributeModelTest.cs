using Landscape.Base.Entity;
using LandscapeRegistry;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class AttributeModelTest
    {
        private NpgsqlConnection conn;

        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();

            var dbcb = new DBConnectionBuilder();
            conn = dbcb.Build(DBSetup.dbName, false, true);

        }

        [TearDown]
        public void TearDown()
        {
            conn.Close();
        }

        [Test]
        public async Task TestAddingUpdatingRemovingAndRenewingOfAttributes()
        {
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            string ciid1;
            using (var trans = conn.BeginTransaction())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await model.CreateCI("H123", trans);
                Assert.AreEqual("H123", ciid1);
                trans.Commit();
            }

            Assert.ThrowsAsync<PostgresException>(async () => await model.CreateCI("H123", null)); // cannot add same identity twice

            long layerID1;
            using (var trans = conn.BeginTransaction())
            {
                var layer1 = await layerModel.CreateLayer("l1", trans);
                layerID1 = layer1.ID;
                Assert.AreEqual(1, layerID1);
                trans.Commit();
            }

            Assert.ThrowsAsync<PostgresException>(async () => await layerModel.CreateLayer("l1", null)); // cannot add same layer twice

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i1 = await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i1.Name);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i2 = await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text2"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i2.Name);

                var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                var aa1 = a1.First();
                Assert.AreEqual(ciid1, aa1.Attribute.CIID);
                //Assert.AreEqual(layerID1, aa1.Attribute.LayerID);
                Assert.AreEqual("a1", aa1.Attribute.Name);
                Assert.AreEqual(AttributeState.Changed, aa1.Attribute.State);
                Assert.AreEqual(AttributeValueTextScalar.Build("text2"), aa1.Attribute.Value);
                Assert.AreEqual(changeset.ID, aa1.Attribute.ChangesetID);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var r1 = await attributeModel.RemoveAttribute("a1", layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", r1.Name);
                Assert.AreEqual(AttributeState.Removed, r1.State);

                var a2 = await attributeModel.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(0, a2.Count());
                var a3 = await attributeModel.GetMergedAttributes("H123", true, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a3.Count());
                var aa3 = a3.First();
                Assert.AreEqual(AttributeState.Removed, aa3.Attribute.State);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i3 = await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text3"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i3.Name);

                var a4 = await attributeModel.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a4.Count());
                var aa4 = a4.First();
                Assert.AreEqual(AttributeState.Renewed, aa4.Attribute.State);
                Assert.AreEqual(AttributeValueTextScalar.Build("text3"), aa4.Attribute.Value);
            }
        }


        [Test]
        public async Task TestAttributeValueMultiplicities()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", null);
            var layer1 = await layerModel.CreateLayer("l1", null);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueTextArray.Build(new string[] { "a", "b", "c" }), layer1.ID, ciid1, changeset.ID, trans);
                var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueTextArray.Build(new string[] { "a", "b", "c" }), a1.First().Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueTextArray.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), layer1.ID, ciid1, changeset.ID, trans);
                var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueTextArray.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), a1.First().Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueIntegerArray.Build(new long[] { 1,2,3,4 }), layer1.ID, ciid1, changeset.ID, trans);
                var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueIntegerArray.Build(new long[] { 1,2,3,4 }), a1.First().Attribute.Value);
            }
        }

        [Test]
        public async Task TestEqualValueInserts()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset2.ID, trans);

            var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeState.New, a1.First().Attribute.State); // second insertAttribute() must not have changed the current entry
        }

        [Test]
        public async Task TestFindAttributesByName()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var ciid2 = await model.CreateCI("H456", trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset1.ID, trans);
            await attributeModel.InsertAttribute("a2", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layer2.ID, ciid1, changeset2.ID, trans);

            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layer2.ID, ciid2, changeset3.ID, trans);
            await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("textL2"), layer2.ID, ciid2, changeset3.ID, trans);

            var a1 = await attributeModel.FindAttributesByName("a%", false, layer1.ID, trans, DateTimeOffset.Now);
            Assert.AreEqual(2, a1.Count());

            var a2 = await attributeModel.FindAttributesByName("a2", false, layer1.ID, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());

            var a3 = await attributeModel.FindAttributesByName("%3", false, layer2.ID, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a3.Count());

            var a4 = await attributeModel.FindAttributesByName("%3", false, layer1.ID, trans, DateTimeOffset.Now);
            Assert.AreEqual(0, a4.Count());

            var a5 = await attributeModel.FindAttributesByName("a1", false, layer2.ID, trans, DateTimeOffset.Now, ciid2);
            Assert.AreEqual(1, a5.Count());
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var ciid2 = await model.CreateCI("H456", trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("prefix1.a1", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset1.ID, trans);
            await attributeModel.InsertAttribute("prefix1.a2", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await attributeModel.InsertAttribute("prefix1.a1", AttributeValueTextScalar.Build("textL2"), layer1.ID, ciid2, changeset2.ID, trans);
            await attributeModel.InsertAttribute("prefix2.a1", AttributeValueTextScalar.Build("textL2"), layer1.ID, ciid2, changeset2.ID, trans);
            await attributeModel.InsertAttribute("prefix1.a3", AttributeValueTextScalar.Build("textL2"), layer1.ID, ciid2, changeset2.ID, trans);

            trans.Commit();

            using var trans2 = conn.BeginTransaction();
            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans2);
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("prefix1", layer1.ID, new BulkCIAttributeDataLayerScope.Fragment[] {
                BulkCIAttributeDataLayerScope.Fragment.Build("a1", AttributeValueTextScalar.Build("textNew"), ciid1),
                BulkCIAttributeDataLayerScope.Fragment.Build("a4", AttributeValueTextScalar.Build("textNew"), ciid2),
                BulkCIAttributeDataLayerScope.Fragment.Build("a2", AttributeValueTextScalar.Build("textNew"), ciid2),
            }), changeset3.ID, trans2);

            var a1 = await attributeModel.FindAttributesByName("prefix1%", false, layer1.ID, trans2, DateTimeOffset.Now);
            Assert.AreEqual(3, a1.Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a2").Count());
            var a2 = await attributeModel.FindAttributesByName("prefix2%", false, layer1.ID, trans2, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());
        }
    }
}
