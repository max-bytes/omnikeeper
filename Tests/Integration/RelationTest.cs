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
    class RelationTest
    {
        [SetUp]
        public void Setup()
        {
            TestDBSetup.Setup();
        }

        [Test]
        public void TestBasics()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(TestDBSetup.dbName, false, true);
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);

            var changesetID = ciModel.CreateChangeset();

            var ciid1 = ciModel.CreateCI("H123");
            var ciid2 = ciModel.CreateCI("H456");
            var ciid3 = ciModel.CreateCI("H789");

            var layerID1 = ciModel.CreateLayer("l1");
            var layerset = new LayerSet(new long[] { layerID1 });

            // test single relation
            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID));
            var r1 = relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(ciid1, rr1.FromCIID);
            Assert.AreEqual(ciid2, rr1.ToCIID);
            Assert.AreEqual(layerID1, rr1.LayerID);
            Assert.AreEqual(RelationState.New, rr1.State);
            Assert.AreEqual(changesetID, rr1.ChangesetID);

            // test repeated insertion
            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID));
            r1 = relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward);
            Assert.AreEqual(1, r1.Count());
            rr1 = r1.First();
            Assert.AreEqual(RelationState.New, rr1.State); // state must still be New


            // test second relation
            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid3, "r1", layerID1, changesetID));
            var r2 = relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward);
            Assert.AreEqual(2, r2.Count());
            var rr2 = r2.Last();
            Assert.AreEqual(ciid1, rr2.FromCIID);
            Assert.AreEqual(ciid3, rr2.ToCIID);
            Assert.AreEqual(layerID1, rr2.LayerID);
            Assert.AreEqual(RelationState.New, rr2.State);
            Assert.AreEqual(changesetID, rr2.ChangesetID);
        }

        [Test]
        public void TestMerging()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(TestDBSetup.dbName, false, true);
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);

            var changesetID = ciModel.CreateChangeset();

            var ciid1 = ciModel.CreateCI("H123");
            var ciid2 = ciModel.CreateCI("H456");

            var layerID1 = ciModel.CreateLayer("l1");
            var layerID2 = ciModel.CreateLayer("l2");
            var layerset = new LayerSet(new long[] { layerID2, layerID1 });

            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID));
            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID2, changesetID));
            var r1 = relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layerID2, rr1.LayerID);
        }

        [Test]
        public void TestRemoveShowsLayerBelow()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(TestDBSetup.dbName, false, true);
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);

            var changesetID = ciModel.CreateChangeset();

            var ciid1 = ciModel.CreateCI("H123");
            var ciid2 = ciModel.CreateCI("H456");

            var layerID1 = ciModel.CreateLayer("l1");
            var layerID2 = ciModel.CreateLayer("l2");
            var layerset = new LayerSet(new long[] { layerID2, layerID1 });

            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID));
            Assert.IsTrue(relationModel.InsertRelation(ciid1, ciid2, "r1", layerID2, changesetID));
            Assert.IsTrue(relationModel.RemoveRelation(ciid1, ciid2, "r1", layerID2, changesetID));

            var r1 = relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layerID1, rr1.LayerID);
        }
    }
}
