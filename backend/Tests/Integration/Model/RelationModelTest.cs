using Landscape.Base.Entity;
using LandscapeRegistry;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
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
using static Landscape.Base.Model.IRelationModel;

namespace Tests.Integration.Model
{
    class RelationModelTest
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
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            string ciid1;
            string ciid3;
            long layerID1;
            Changeset changeset;
            using (var trans = conn.BeginTransaction())
            {
                changeset = await changesetModel.CreateChangeset(user.ID, trans);

                ciid1 = await ciModel.CreateCI("H123", trans);
                var ciid2 = await ciModel.CreateCI("H456", trans);
                ciid3 = await ciModel.CreateCI("H789", trans);
                var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", PredicateState.Active, trans);

                var layer1 = await layerModel.CreateLayer("l1", trans);
                layerID1 = layer1.ID;
                var layerset = new LayerSet(new long[] { layerID1 });

                // test single relation
                var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset.ID, trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                var r1 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(ciid1, rr1.FromCIID);
                Assert.AreEqual(ciid2, rr1.ToCIID);
                Assert.AreEqual(layerID1, rr1.LayerID);
                Assert.AreEqual(RelationState.New, rr1.State);
                Assert.AreEqual(changeset.ID, rr1.ChangesetID);

                // test repeated insertion
                var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset.ID, trans);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                r1 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
                Assert.AreEqual(1, r1.Count());
                rr1 = r1.First();
                Assert.AreEqual(RelationState.New, rr1.State); // state must still be New


                // test second relation
                var i3 = await relationModel.InsertRelation(ciid1, ciid3, predicate1.ID, layerID1, changeset.ID, trans);
                Assert.AreEqual(predicate1.ID, i3.PredicateID);
                var r2 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
                Assert.AreEqual(2, r2.Count());
                var rr2 = r2.Last();
                Assert.AreEqual(ciid1, rr2.FromCIID);
                Assert.AreEqual(ciid3, rr2.ToCIID);
                Assert.AreEqual(layerID1, rr2.LayerID);
                Assert.AreEqual(RelationState.New, rr2.State);
                Assert.AreEqual(changeset.ID, rr2.ChangesetID);
            }

            using (var trans2 = conn.BeginTransaction())
            {
                Assert.ThrowsAsync<KeyNotFoundException>(async () => await relationModel.InsertRelation(ciid1, ciid3, "unknown predicate ID", layerID1, changeset.ID, trans2));
            }
        }

        [Test]
        public async Task TestMerging()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn); 
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var changeset = await changesetModel.CreateChangeset(user.ID, trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", PredicateState.Active, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset.ID, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset.ID, trans);
            Assert.AreEqual(predicate1.ID, i1.PredicateID);
            Assert.AreEqual(predicate1.ID, i2.PredicateID);
            var r1 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layer2.ID, rr1.LayerID);
        }


        [Test]
        public async Task TestGetRelationsByPredicate()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var changeset = await changesetModel.CreateChangeset(user.ID, trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var ciid3 = await ciModel.CreateCI("H789", trans);
            var predicateID1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", PredicateState.Active, trans);
            var predicateID2 = await predicateModel.InsertOrUpdate("predicate_2", "", "", PredicateState.Active, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset.ID, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicateID2.ID, layer1.ID, changeset.ID, trans);
            var i3 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset.ID, trans);
            var i4 = await relationModel.InsertRelation(ciid3, ciid1, predicateID2.ID, layer1.ID, changeset.ID, trans);

            var r1 = await relationModel.GetRelationsWithPredicateID(layerset, false, predicateID1.ID, trans);
            Assert.AreEqual(2, r1.Count());
            Assert.IsFalse(r1.Any(r => r.PredicateID == predicateID2.ID));
            var r2 = await relationModel.GetRelationsWithPredicateID(layerset, false, predicateID2.ID, trans);
            Assert.AreEqual(2, r2.Count());
            Assert.IsFalse(r2.Any(r => r.PredicateID == predicateID1.ID));
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);

            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var layer1 = await layerModel.CreateLayer("l1", null);
            var layer2 = await layerModel.CreateLayer("l2", null);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });
            var ciid1 = await ciModel.CreateCI("H123", null);
            var ciid2 = await ciModel.CreateCI("H456", null);
            var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", PredicateState.Active, null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset.ID, trans);
                var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset.ID, trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                var removedRelation = await relationModel.RemoveRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset.ID, trans);
                Assert.IsNotNull(removedRelation);
                var r1 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(layer1.ID, rr1.LayerID);
                trans.Commit();
            }

            // add relation again
            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset.ID, trans);
                var r2 = await relationModel.GetMergedRelations("H123", false, layerset, IncludeRelationDirections.Forward, trans);
                Assert.AreEqual(1, r2.Count());
                var rr2 = r2.First();
                Assert.AreEqual(layer2.ID, rr2.LayerID);
                trans.Commit();
            }
        }


        [Test]
        public async Task TestBulkReplace()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            using var trans = conn.BeginTransaction();
            var changeset = await changesetModel.CreateChangeset(user.ID, trans);

            var ciid1 = await ciModel.CreateCI("H123", trans);
            var ciid2 = await ciModel.CreateCI("H456", trans);
            var ciid3 = await ciModel.CreateCI("H789", trans);
            var predicateID1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", PredicateState.Active, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset.ID, trans);
            var i2 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset.ID, trans);
            var i3 = await relationModel.InsertRelation(ciid1, ciid3, predicateID1.ID, layer1.ID, changeset.ID, trans);
            trans.Commit();

            // test bulk replace
            using var trans2 = conn.BeginTransaction();
            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans2);
            await relationModel.BulkReplaceRelations(BulkRelationData.Build(predicateID1.ID, layer1.ID, new (string, string)[] {
                    (ciid1, ciid2),
                    (ciid2, ciid1),
                    (ciid3, ciid2),
                    (ciid3, ciid1)
                }), changeset2.ID, trans2);

            var r1 = await relationModel.GetRelationsWithPredicateID(layerset, false, predicateID1.ID, trans2);
            Assert.AreEqual(4, r1.Count());
        }
    }
}
