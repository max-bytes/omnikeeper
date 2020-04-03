﻿using LandscapeRegistry;
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
        public async Task TestGetCIs()
        {
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
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
                var i1 = await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("text1"), layerID1, ciid1, changeset.ID, trans);
                var i2 = await attributeModel.InsertAttribute("a2", AttributeValueTextScalar.Build("text1"), layerID1, ciid2, changeset.ID, trans);
                var i3 = await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("text1"), layerID2, ciid1, changeset.ID, trans);

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
            var attributeModel = new AttributeModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
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
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset.ID, trans);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid1, changeset.ID, trans);

            var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a1.First().Attribute.Value);

            var a2 = await attributeModel.GetMergedAttributes("H123", false, layerset2, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL2"), a2.First().Attribute.Value);

            var a3 = await attributeModel.GetMergedAttributes("H123", false, layerset3, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a3.First().Attribute.Value);

            var a4 = await attributeModel.GetMergedAttributes("H123", false, layerset4, trans, DateTimeOffset.Now);
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(AttributeValueTextScalar.Build("textL2"), a4.First().Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var model = new CIModel(attributeModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await model.CreateCI("H123", null);
            var layerID1 = await layerModel.CreateLayer("l1", null);
            var layerID2 = await layerModel.CreateLayer("l2", null);
            var layerset1 = new LayerSet(new long[] { layerID2, layerID1 });

            using (var trans = conn.BeginTransaction())
            {

                var changeset1 = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layerID1, ciid1, changeset1.ID, trans);

                var changeset2 = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL2"), layerID2, ciid1, changeset2.ID, trans);

                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset3 = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.RemoveAttribute("a1", layerID2, ciid1, changeset3.ID, trans);
                trans.Commit();
            }

            var a1 = await attributeModel.GetMergedAttributes("H123", false, layerset1, null, DateTimeOffset.Now);
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(AttributeValueTextScalar.Build("textL1"), a1.First().Attribute.Value);
        }


        [Test]
        public async Task TestCITypes()
        {
            var attributeModel = new AttributeModel(conn);
            var model = new CIModel(attributeModel, conn);
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
    }
}
