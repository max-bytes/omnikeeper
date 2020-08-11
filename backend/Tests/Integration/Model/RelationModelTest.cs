using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
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
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, new BaseRelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn), conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            Guid ciid1;
            Guid ciid3;
            long layerID1;
            ChangesetProxy changeset;
            using (var trans = conn.BeginTransaction())
            {
                changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

                ciid1 = await ciModel.CreateCI(trans);
                var ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

                var layer1 = await layerModel.CreateLayer("l1", trans);
                layerID1 = layer1.ID;
                var layerset = new LayerSet(new long[] { layerID1 });

                // test single relation
                var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset, trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                var r1 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(ciid1, rr1.Relation.FromCIID);
                Assert.AreEqual(ciid2, rr1.Relation.ToCIID);
                Assert.AreEqual(layerID1, rr1.LayerID);
                Assert.AreEqual(RelationState.New, rr1.Relation.State);
                Assert.AreEqual((await changeset.GetChangeset(trans)).ID, rr1.Relation.ChangesetID);

                // test repeated insertion
                var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset, trans);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                r1 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                rr1 = r1.First();
                Assert.AreEqual(RelationState.New, rr1.Relation.State); // state must still be New


                // test second relation
                var i3 = await relationModel.InsertRelation(ciid1, ciid3, predicate1.ID, layerID1, changeset, trans);
                Assert.AreEqual(predicate1.ID, i3.PredicateID);
                var r2 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, r2.Count());
                var rr2 = r2.FirstOrDefault(r => r.Relation.ToCIID == ciid3);
                Assert.AreEqual(ciid1, rr2.Relation.FromCIID);
                Assert.IsNotNull(rr2);
                Assert.AreEqual(layerID1, rr2.LayerID);
                Assert.AreEqual(RelationState.New, rr2.Relation.State);
                Assert.AreEqual((await changeset.GetChangeset(trans)).ID, rr2.Relation.ChangesetID);
            }

            using (var trans2 = conn.BeginTransaction())
            {
                Assert.ThrowsAsync<KeyNotFoundException>(async () => await relationModel.InsertRelation(ciid1, ciid3, "unknown predicate ID", layerID1, changeset, trans2));
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
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, new BaseRelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn), conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, trans);
            Assert.AreEqual(predicate1.ID, i1.PredicateID);
            Assert.AreEqual(predicate1.ID, i2.PredicateID);
            var r1 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
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
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, new BaseRelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn), conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);
            var predicateID1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID2 = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset, trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicateID2.ID, layer1.ID, changeset, trans);
            var i3 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset, trans);
            var i4 = await relationModel.InsertRelation(ciid3, ciid1, predicateID2.ID, layer1.ID, changeset, trans);

            var r1 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID1.ID), false, layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r1.Count());
            Assert.IsFalse(r1.Any(r => r.Relation.PredicateID == predicateID2.ID));
            var r2 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID2.ID), false, layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r2.Count());
            Assert.IsFalse(r2.Any(r => r.Relation.PredicateID == predicateID1.ID));
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);

            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, new BaseRelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn), conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var layer1 = await layerModel.CreateLayer("l1", null);
            var layer2 = await layerModel.CreateLayer("l2", null);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });
            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, null);

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset, trans);
                var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                trans.Commit();
            }

            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                var removedRelation = await relationModel.RemoveRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, trans);
                Assert.IsNotNull(removedRelation);
                var r1 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(layer1.ID, rr1.LayerID);
                trans.Commit();
            }

            // add relation again
            using (var trans = conn.BeginTransaction())
            {
                var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, trans);
                var r2 = await relationModel.GetMergedRelations(new RelationSelectionFromTo(ciid1, null), false, layerset, trans, TimeThreshold.BuildLatest());
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
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(MockedEmptyOnlineAccessProxy.O, new BaseRelationModel(MockedEmptyOnlineAccessProxy.O, predicateModel, conn), conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            using var trans = conn.BeginTransaction();
            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);
            var predicateID1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset, trans);
            var i2 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset, trans);
            var i3 = await relationModel.InsertRelation(ciid1, ciid3, predicateID1.ID, layer1.ID, changeset, trans);
            trans.Commit();

            // test bulk replace
            using var trans2 = conn.BeginTransaction();
            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await relationModel.BulkReplaceRelations(BulkRelationDataPredicateScope.Build(predicateID1.ID, layer1.ID, new BulkRelationDataPredicateScope.Fragment[] {
                    BulkRelationDataPredicateScope.Fragment.Build(ciid1, ciid2),
                    BulkRelationDataPredicateScope.Fragment.Build(ciid2, ciid1),
                    BulkRelationDataPredicateScope.Fragment.Build(ciid3, ciid2),
                    BulkRelationDataPredicateScope.Fragment.Build(ciid3, ciid1)
                }), changeset2, trans2);

            var r1 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID1.ID), false, layerset, trans2, TimeThreshold.BuildLatest());
            Assert.AreEqual(4, r1.Count());
        }
    }
}
