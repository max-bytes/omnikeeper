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
    class CIModelTest
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
            var model = new CIModel(conn);
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
                layerID1 = await layerModel.CreateLayer("l1", trans);
                Assert.AreEqual(1, layerID1);
                trans.Commit();
            }

            Assert.ThrowsAsync<PostgresException>(async () => await layerModel.CreateLayer("l1", null)); // cannot add same layer twice

            var layerset = await layerModel.BuildLayerSet(new string[] { "l1" }, null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i1 = await model.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i1.Name);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i2 = await model.InsertAttribute("a1", AttributeValueTextScalar.Build("text2"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i2.Name);

                var a1 = await model.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
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
                var r1 = await model.RemoveAttribute("a1", layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", r1.Name);
                Assert.AreEqual(AttributeState.Removed, r1.State);

                var a2 = await model.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(0, a2.Count());
                var a3 = await model.GetMergedAttributes("H123", true, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a3.Count());
                var aa3 = a3.First();
                Assert.AreEqual(AttributeState.Removed, aa3.Attribute.State);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i3 = await model.InsertAttribute("a1", AttributeValueTextScalar.Build("text3"), layerID1, ciid1, changeset.ID, trans);
                Assert.AreEqual("a1", i3.Name);

                var a4 = await model.GetMergedAttributes("H123", false, layerset, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a4.Count());
                var aa4 = a4.First();
                Assert.AreEqual(AttributeState.Renewed, aa4.Attribute.State);
                Assert.AreEqual(AttributeValueTextScalar.Build("text3"), aa4.Attribute.Value);
            }
        }


        [Test]
        public async Task TestGetCIs()
        {
            var model = new CIModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            string ciid1;
            string ciid2;
            string ciid3;
            using (var trans = conn.BeginTransaction())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                var ciTypeID1 = await model.CreateCIType("T1", trans);
                ciid1 = await model.CreateCIWithType("H123", ciTypeID1, trans);
                ciid2 = await model.CreateCIWithType("H456", ciTypeID1, trans);
                ciid3 = await model.CreateCIWithType("H789", ciTypeID1, trans);
                trans.Commit();
            }

            long layerID1;
            long layerID2;
            using (var trans = conn.BeginTransaction())
            {
                layerID1 = await layerModel.CreateLayer("l1", trans);
                layerID2 = await layerModel.CreateLayer("l2", trans);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i1 = await model.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layerID1, ciid1, changeset.ID, trans);
                var i2 = await model.InsertAttribute("a2", AttributeValueTextScalar.Build("text1"), layerID1, ciid2, changeset.ID, trans);
                var i3 = await model.InsertAttribute("a3", AttributeValueTextScalar.Build("text1"), layerID2, ciid1, changeset.ID, trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var cis1 = await model.GetCIs(layerID1, false, trans, DateTimeOffset.Now);
                Assert.AreEqual(2, cis1.Count());
                Assert.AreEqual(1, cis1.Count(c => c.Identity == ciid1 && c.Attributes.Any(a => a.Name == "a1")));
                Assert.AreEqual(1, cis1.Count(c => c.Identity == ciid2 && c.Attributes.Any(a => a.Name == "a2")));
                var cis2 = await model.GetCIs(layerID2, false, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, cis2.Count());
                Assert.AreEqual(1, cis2.Count(c => c.Identity == ciid1 && c.Attributes.Any(a => a.Name == "a3")));
                var cis3 = await model.GetCIs(layerID2, true, trans, DateTimeOffset.Now);
                Assert.AreEqual(3, cis3.Count());
                Assert.AreEqual(1, cis3.Count(c => c.Identity == ciid1 && c.Attributes.Any(a => a.Name == "a3")));
                Assert.AreEqual(1, cis3.Count(c => c.Identity == ciid2 && c.Attributes.Count() == 0));
                Assert.AreEqual(1, cis3.Count(c => c.Identity == ciid3 && c.Attributes.Count() == 0));

                trans.Commit();
            }
        }

        [Test]
        public async Task TestLayerSets()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var layerID1 = await layerModel.CreateLayer("l1", trans);
            var layerID2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layerID1 });
            var layerset2 = new LayerSet(new long[] { layerID2 });
            var layerset3 = new LayerSet(new long[] { layerID1, layerID2 });
            var layerset4 = new LayerSet(new long[] { layerID2, layerID1 });

            var changeset = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid1, changeset.ID, trans);

            var a1 = await model.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a1.First().Attribute.Value);

            var a2 = await model.GetMergedAttributes("H123", false, layerset2, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL2"), a2.First().Attribute.Value);

            var a3 = await model.GetMergedAttributes("H123", false, layerset3, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a3.First().Attribute.Value);

            var a4 = await model.GetMergedAttributes("H123", false, layerset4, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL2"), a4.First().Attribute.Value);
        }

        [Test]
        public async Task TestAttributeValueMultiplicities()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", null);
            var layerID1 = await layerModel.CreateLayer("l1", null);

            var layerset1 = new LayerSet(new long[] { layerID1 });

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await model.InsertAttribute("a1", AttributeValueTextArray.Build(new string[] { "a", "b", "c" }), layerID1, ciid1, changeset.ID, trans);
                var a1 = await model.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueTextArray.Build(new string[] { "a", "b", "c" }), a1.First().Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await model.InsertAttribute("a1", AttributeValueTextArray.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), layerID1, ciid1, changeset.ID, trans);
                var a1 = await model.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueTextArray.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), a1.First().Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await model.InsertAttribute("a1", AttributeValueIntegerArray.Build(new long[] { 1,2,3,4 }), layerID1, ciid1, changeset.ID, trans);
                var a1 = await model.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueIntegerArray.Build(new long[] { 1,2,3,4 }), a1.First().Attribute.Value);
            }
        }

        [Test]
        public async Task TestEqualValueInserts()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var layerID1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layerID1 });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset2.ID, trans);

            var a1 = await model.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeState.New, a1.First().Attribute.State); // second insertAttribute() must not have changed the current entry
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", null);
            var layerID1 = await layerModel.CreateLayer("l1", null);
            var layerID2 = await layerModel.CreateLayer("l2", null);
            var layerset1 = new LayerSet(new long[] { layerID2, layerID1 });

            using (var trans = conn.BeginTransaction())
            {

                var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
                await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);

                var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
                await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid1, changeset2.ID, trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset3 = await changesetModel.CreateChangeset(user.ID, trans);
                await model.RemoveAttribute("a1", layerID2, ciid1, changeset3.ID, trans);
                trans.Commit();
            }

            var a1 = await model.GetMergedAttributes("H123", false, layerset1, null, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a1.First().Attribute.Value);
        }

        [Test]
        public async Task TestFindAttributesByName()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var ciid2 = await model.CreateCI("H456", trans);
            var layerID1 = await layerModel.CreateLayer("l1", trans);
            var layerID2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layerID2, layerID1 });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);
            await model.InsertAttribute("a2", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid1, changeset2.ID, trans);

            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid2, changeset3.ID, trans);
            await model.InsertAttribute("a3", AttributeValueTextScalar.Build("textL2"), layerID2, ciid2, changeset3.ID, trans);

            var a1 = await model.FindAttributesByName("a%", false, layerID1, trans, DateTimeOffset.Now);
            Assert.AreEqual(2, a1.Count());

            var a2 = await model.FindAttributesByName("a2", false, layerID1, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());

            var a3 = await model.FindAttributesByName("%3", false, layerID2, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a3.Count());

            var a4 = await model.FindAttributesByName("%3", false, layerID1, trans, DateTimeOffset.Now);
            Assert.AreEqual(0, a4.Count());

            var a5 = await model.FindAttributesByName("a1", false, layerID2, trans, DateTimeOffset.Now, ciid2);
            Assert.AreEqual(1, a5.Count());
        }


        [Test]
        public async Task TestCITypes()
        {
            var model = new CIModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            using (var trans = conn.BeginTransaction())
            {
                // test setting and getting of citype
                var ciTypeID1 = await model.CreateCIType("T1", trans);
                Assert.AreEqual("T1", (await model.GetCIType("T1", trans)).ID);

                // test CI creation
                var ciid1 = await model.CreateCIWithType("H123", ciTypeID1, trans);
                Assert.AreEqual("H123", ciid1);
                var ciType = await model.GetTypeOfCI("H123", trans, null);
                Assert.AreEqual("T1", ciType.ID);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                Assert.ThrowsAsync<Exception>(async () => await model.CreateCIWithType("H456", "T-Nonexisting", trans));
            }

            using (var trans = conn.BeginTransaction())
            {
                // test overriding of type
                var ciTypeID2 = await model.CreateCIType("T2", trans);
                await model.SetCIType("H123", "T2", trans);
                var ciType = await model.GetTypeOfCI("H123", trans, null);
                Assert.AreEqual("T2", ciType.ID);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                // test getting by ci type
                var layerID1 = await layerModel.CreateLayer("l1", trans);
                var layerset1 = new LayerSet(new long[] { layerID1 });
                var ciid2 = await model.CreateCIWithType("H456", "T1", trans);
                var ciid3 = await model.CreateCIWithType("H789", "T2", trans);
                Assert.AreEqual(1, (await model.GetMergedCIsByType(layerset1, trans, DateTimeOffset.Now, "T1")).Count());
                Assert.AreEqual(2, (await model.GetMergedCIsByType(layerset1, trans, DateTimeOffset.Now, "T2")).Count());
            }
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", trans);
            var ciid2 = await model.CreateCI("H456", trans);
            var layerID1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layerID1 });

            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("prefix1.a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);
            await model.InsertAttribute("prefix1.a2", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);

            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
            await model.InsertAttribute("prefix1.a1", AttributeValueTextScalar.Build("textL2"), layerID1, ciid2, changeset2.ID, trans);
            await model.InsertAttribute("prefix2.a1", AttributeValueTextScalar.Build("textL2"), layerID1, ciid2, changeset2.ID, trans);
            await model.InsertAttribute("prefix1.a3", AttributeValueTextScalar.Build("textL2"), layerID1, ciid2, changeset2.ID, trans);

            trans.Commit();

            using var trans2 = conn.BeginTransaction();
            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans2);
            await model.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("prefix1", layerID1, new BulkCIAttributeDataLayerScope.Fragment[] {
                BulkCIAttributeDataLayerScope.Fragment.Build("a1", AttributeValueTextScalar.Build("textNew"), ciid1),
                BulkCIAttributeDataLayerScope.Fragment.Build("a4", AttributeValueTextScalar.Build("textNew"), ciid2),
                BulkCIAttributeDataLayerScope.Fragment.Build("a2", AttributeValueTextScalar.Build("textNew"), ciid2),
            }), changeset3.ID, trans2);

            var a1 = await model.FindAttributesByName("prefix1%", false, layerID1, trans2, DateTimeOffset.Now);
            Assert.AreEqual(3, a1.Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a2").Count());
            var a2 = await model.FindAttributesByName("prefix2%", false, layerID1, trans2, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());
        }
    }
}
