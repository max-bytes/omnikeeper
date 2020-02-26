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
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class RelationTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task TestBasics()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);
            var layerModel = new LayerModel(conn);

            var changesetID = await ciModel.CreateChangeset(trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var ciid3 = await ciModel.CreateCI("H789", trans);

            var layerID1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layerID1 });

            // test single relation
            var i1 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID, trans);
            Assert.AreEqual("r1", i1.Predicate);
            var r1 = await relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(ciid1, rr1.FromCIID);
            Assert.AreEqual(ciid2, rr1.ToCIID);
            Assert.AreEqual(layerID1, rr1.LayerID);
            Assert.AreEqual(RelationState.New, rr1.State);
            Assert.AreEqual(changesetID, rr1.ChangesetID);

            // test repeated insertion
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID, trans);
            Assert.AreEqual("r1", i2.Predicate);
            r1 = await relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(1, r1.Count());
            rr1 = r1.First();
            Assert.AreEqual(RelationState.New, rr1.State); // state must still be New


            // test second relation
            var i3 = await relationModel.InsertRelation(ciid1, ciid3, "r1", layerID1, changesetID, trans);
            Assert.AreEqual("r1", i3.Predicate);
            var r2 = await relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(2, r2.Count());
            var rr2 = r2.Last();
            Assert.AreEqual(ciid1, rr2.FromCIID);
            Assert.AreEqual(ciid3, rr2.ToCIID);
            Assert.AreEqual(layerID1, rr2.LayerID);
            Assert.AreEqual(RelationState.New, rr2.State);
            Assert.AreEqual(changesetID, rr2.ChangesetID);
        }

        [Test]
        public async Task TestMerging()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);
            var layerModel = new LayerModel(conn);

            var changesetID = await ciModel.CreateChangeset(trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);

            var layerID1 = await layerModel.CreateLayer("l1", trans);
            var layerID2 = await layerModel.CreateLayer("l2", trans);
            var layerset = new LayerSet(new long[] { layerID2, layerID1 });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID2, changesetID, trans);
            Assert.AreEqual("r1", i1.Predicate);
            Assert.AreEqual("r1", i2.Predicate);
            var r1 = await relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layerID2, rr1.LayerID);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var ciModel = new CIModel(conn);
            var relationModel = new RelationModel(conn);
            var layerModel = new LayerModel(conn);

            var changesetID = await ciModel.CreateChangeset(trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);

            var layerID1 = await layerModel.CreateLayer("l1", trans);
            var layerID2 = await layerModel.CreateLayer("l2", trans);
            var layerset = new LayerSet(new long[] { layerID2, layerID1 });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID1, changesetID, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, "r1", layerID2, changesetID, trans);
            Assert.AreEqual("r1", i1.Predicate);
            Assert.AreEqual("r1", i2.Predicate);
            Assert.IsTrue(await relationModel.RemoveRelation(ciid1, ciid2, "r1", layerID2, changesetID, trans));

            var r1 = await relationModel.GetMergedRelations("H123", false, layerset, RelationModel.IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layerID1, rr1.LayerID);
        }
    }
}
