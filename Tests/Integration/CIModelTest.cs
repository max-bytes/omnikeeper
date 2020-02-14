using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
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
        [SetUp]
        public void Setup()
        {
            TestDBSetup.Setup();
        }

        [Test]
        public void TestAddingUpdatingRemovingAndRenewingOfAttributes()
        {
            var dbcb = new DBConnectionBuilder();
            var conn = dbcb.Build(TestDBSetup.dbName);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);

            var changesetID = model.CreateChangeset();

            var ciid1 = model.CreateCI("H123");
            Assert.AreEqual(1, ciid1);
            Assert.Throws<PostgresException>(() => model.CreateCI("H123")); // cannot add same identity twice

            var layerID1 = model.CreateLayer("l1");
            Assert.AreEqual(1, layerID1);
            Assert.Throws<PostgresException>(() => model.CreateLayer("l1")); // cannot add same layer twice

            var layerset = layerModel.BuildLayerSet(new string[] { "l1" });

            Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text1"), layerID1, "H123", changesetID));
            Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text2"), layerID1, "H123", changesetID));

            var a1 = model.GetMergedAttributes("H123", false, layerset);
            Assert.AreEqual(1, a1.Count());
            var aa1 = a1.First();
            Assert.AreEqual(ciid1, aa1.CIID);
            Assert.AreEqual(layerID1, aa1.LayerID);
            Assert.AreEqual("a1", aa1.Name);
            Assert.AreEqual(AttributeState.Changed, aa1.State);
            Assert.AreEqual(AttributeValueText.Build("text2"), aa1.Value);

            Assert.IsTrue(model.RemoveAttribute("a1", layerID1, "H123", changesetID));

            var a2 = model.GetMergedAttributes("H123", false, layerset);
            Assert.AreEqual(0, a2.Count());
            var a3 = model.GetMergedAttributes("H123", true, layerset);
            Assert.AreEqual(1, a3.Count());
            var aa3 = a3.First();
            Assert.AreEqual(AttributeState.Removed, aa3.State);

            Assert.IsTrue(model.InsertAttribute("a1", AttributeValueText.Build("text3"), layerID1, "H123", changesetID));

            var a4 = model.GetMergedAttributes("H123", false, layerset);
            Assert.AreEqual(1, a4.Count());
            var aa4 = a4.First();
            Assert.AreEqual(AttributeState.Renewed, aa4.State);
            Assert.AreEqual(AttributeValueText.Build("text3"), aa4.Value);
        }


        [Test]
        public void TestLayerSets()
        {
            var dbcb = new DBConnectionBuilder();
            var conn = dbcb.Build(TestDBSetup.dbName);
            var model = new CIModel(conn);

            var ciid1 = model.CreateCI("H123");
            var layerID1 = model.CreateLayer("l1");
            var layerID2 = model.CreateLayer("l2");

            var layerset1 = new LayerSet(new long[] { layerID1 });
            var layerset2 = new LayerSet(new long[] { layerID2 });
            var layerset3 = new LayerSet(new long[] { layerID1, layerID2 });
            var layerset4 = new LayerSet(new long[] { layerID2, layerID1 });

            var changesetID = model.CreateChangeset();
            model.InsertAttribute("a1", AttributeValueText.Build("textL1"), layerID1, "H123", changesetID);
            model.InsertAttribute("a1", AttributeValueText.Build("textL2"), layerID2, "H123", changesetID);

            var a1 = model.GetMergedAttributes("H123", false, layerset1);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeValueText.Build("textL1"), a1.First().Value);

            var a2 = model.GetMergedAttributes("H123", false, layerset2);
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(AttributeValueText.Build("textL2"), a2.First().Value);

            var a3 = model.GetMergedAttributes("H123", false, layerset3);
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(AttributeValueText.Build("textL1"), a3.First().Value);

            var a4 = model.GetMergedAttributes("H123", false, layerset4);
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(AttributeValueText.Build("textL2"), a4.First().Value);
        }

        [Test]
        public void TestEqualValueInserts()
        {
            var dbcb = new DBConnectionBuilder();
            var conn = dbcb.Build(TestDBSetup.dbName);
            var model = new CIModel(conn);

            var ciid1 = model.CreateCI("H123");
            var layerID1 = model.CreateLayer("l1");

            var layerset1 = new LayerSet(new long[] { layerID1 });

            var changesetID1 = model.CreateChangeset();
            model.InsertAttribute("a1", AttributeValueText.Build("textL1"), layerID1, "H123", changesetID1);

            var changesetID2 = model.CreateChangeset();
            model.InsertAttribute("a1", AttributeValueText.Build("textL1"), layerID1, "H123", changesetID2);

            var a1 = model.GetMergedAttributes("H123", false, layerset1);
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(AttributeState.New, a1.First().State); // second insertAttribute() must not have changed the current entry
        }
    }
}
