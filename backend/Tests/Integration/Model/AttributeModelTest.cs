﻿using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using Moq;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;

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
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var model = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);
            Guid ciid1;
            using (var trans = conn.BeginTransaction())
            {
                var changesetID = await changesetModel.CreateChangeset(user.ID, trans);
                ciid1 = await model.CreateCI(trans);
                trans.Commit();
            }

            Assert.ThrowsAsync<PostgresException>(async () => await model.CreateCI(ciid1, null)); // cannot add same identity twice

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
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var i1 = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text1"), ciid1, layerID1, changeset, trans);
                Assert.AreEqual("a1", i1.Name);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var i2 = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text2"), ciid1, layerID1, changeset, trans);
                Assert.AreEqual("a1", i2.Name);

                var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, a1.Count());
                var aa1 = a1.First().Value;
                Assert.AreEqual(ciid1, aa1.Attribute.CIID);
                //Assert.AreEqual(layerID1, aa1.Attribute.LayerID);
                Assert.AreEqual("a1", aa1.Attribute.Name);
                Assert.AreEqual(AttributeState.Changed, aa1.Attribute.State);
                Assert.AreEqual(AttributeScalarValueText.Build("text2"), aa1.Attribute.Value);
                Assert.AreEqual((await changeset.GetChangeset(trans)).ID, aa1.Attribute.ChangesetID);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var r1 = await attributeModel.RemoveAttribute("a1", ciid1, layerID1, changeset, trans);
                Assert.AreEqual("a1", r1.Name);
                Assert.AreEqual(AttributeState.Removed, r1.State);

                var a2 = await attributeModel.GetMergedAttributes(ciid1, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(0, a2.Count());
                var aa3 = await attributeModel.GetAttribute("a1", ciid1, layerID1, trans, TimeThreshold.BuildLatest());
                Assert.IsNotNull(aa3);
                Assert.AreEqual(AttributeState.Removed, aa3.State);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var i3 = await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("text3"), ciid1, layerID1, changeset, trans);
                Assert.AreEqual("a1", i3.Name);

                var a4 = await attributeModel.GetMergedAttributes(ciid1, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, a4.Count());
                var aa4 = a4.First().Value;
                Assert.AreEqual(AttributeState.Renewed, aa4.Attribute.State);
                Assert.AreEqual(AttributeScalarValueText.Build("text3"), aa4.Attribute.Value);
            }
        }


        [Test]
        public async Task TestAttributeValueMultiplicities()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(null);
            var layer1 = await layerModel.CreateLayer("l1", null);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueText.Build(new string[] { "a", "b", "c" }), ciid1, layer1.ID, changeset, trans);
                var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueText.Build(new string[] { "a", "b", "c" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeArrayValueText.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), ciid1, layer1.ID, changeset, trans);
                var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeArrayValueText.Build(new string[] { "a,", "b,b", ",c", "\\d", "\\,e" }), a1.First().Value.Attribute.Value);
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await attributeModel.InsertAttribute("a1", AttributeValueIntegerArray.Build(new long[] { 1, 2, 3, 4 }), ciid1, layer1.ID, changeset, trans);
                var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, a1.Count());
                Assert.AreEqual(AttributeValueIntegerArray.Build(new long[] { 1, 2, 3, 4 }), a1.First().Value.Attribute.Value);
            }
        }

        [Test]
        public async Task TestEqualValueInserts()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset1, trans);

            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset2, trans);

            var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeState.New, a1.First().Value.Attribute.State); // second insertAttribute() must not have changed the current entry
        }

        [Test]
        public async Task TestFindAttributesByName()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(trans);
            var ciid2 = await model.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);

            var layerset1 = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset1, trans);
            await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset1, trans);

            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL2"), ciid1, layer2.ID, changeset2, trans);

            var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL2"), ciid2, layer2.ID, changeset3, trans);
            await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("textL2"), ciid2, layer2.ID, changeset3, trans);

            var a1 = await attributeModel.FindAttributesByName("^a", new AllCIIDsSelection(), layer1.ID, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, a1.Count());

            var a2 = await attributeModel.FindAttributesByName("^a2$", new AllCIIDsSelection(), layer1.ID, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a2.Count());

            var a3 = await attributeModel.FindAttributesByName("3$", new AllCIIDsSelection(), layer2.ID, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a3.Count());

            var a4 = await attributeModel.FindAttributesByName("^3", new AllCIIDsSelection(), layer1.ID, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, a4.Count());

            var a5 = await attributeModel.FindAttributesByName("^a1$", SpecificCIIDsSelection.Build(ciid2), layer2.ID, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a5.Count());
        }

        [Test]
        public async Task TestBulkReplace()
        {
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            using var trans = conn.BeginTransaction();
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI(trans);
            var ciid2 = await model.CreateCI(trans);
            var layer1 = await layerModel.CreateLayer("l1", trans);

            var layerset1 = new LayerSet(new long[] { layer1.ID });

            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("prefix1.a1", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset1, trans);
            await attributeModel.InsertAttribute("prefix1.a2", AttributeScalarValueText.Build("textL1"), ciid1, layer1.ID, changeset1, trans);

            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("prefix1.a1", AttributeScalarValueText.Build("textL2"), ciid2, layer1.ID, changeset2, trans);
            await attributeModel.InsertAttribute("prefix2.a1", AttributeScalarValueText.Build("textL2"), ciid2, layer1.ID, changeset2, trans);
            await attributeModel.InsertAttribute("prefix1.a3", AttributeScalarValueText.Build("textL2"), ciid2, layer1.ID, changeset2, trans);

            trans.Commit();

            using var trans2 = conn.BeginTransaction();
            var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.BulkReplaceAttributes(BulkCIAttributeDataLayerScope.Build("prefix1.", layer1.ID, new BulkCIAttributeDataLayerScope.Fragment[] {
                BulkCIAttributeDataLayerScope.Fragment.Build("a1", AttributeScalarValueText.Build("textNew"), ciid1),
                BulkCIAttributeDataLayerScope.Fragment.Build("a4", AttributeScalarValueText.Build("textNew"), ciid2),
                BulkCIAttributeDataLayerScope.Fragment.Build("a2", AttributeScalarValueText.Build("textNew"), ciid2),
            }), changeset3, trans2);

            var a1 = await attributeModel.FindAttributesByName("^prefix1", new AllCIIDsSelection(), layer1.ID, trans2, TimeThreshold.BuildLatest());
            Assert.AreEqual(3, a1.Count());
            Assert.AreEqual(1, a1.Where(a => a.Name == "prefix1.a2").Count());
            var a2 = await attributeModel.FindAttributesByName("^prefix2", new AllCIIDsSelection(), layer1.ID, trans2, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a2.Count());
        }
    }
}
