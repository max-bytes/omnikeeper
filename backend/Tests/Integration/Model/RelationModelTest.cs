using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class RelationModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var layerModel = new LayerModel();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());

            Guid ciid1;
            Guid ciid3;
            long layerID1;
            ChangesetProxy changeset;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

                ciid1 = await ciModel.CreateCI(trans);
                var ciid2 = await ciModel.CreateCI(trans);
                ciid3 = await ciModel.CreateCI(trans);
                var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

                var layer1 = await layerModel.CreateLayer("l1", trans);
                layerID1 = layer1.ID;
                var layerset = new LayerSet(new long[] { layerID1 });

                // test single relation
                var (i1, c1) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                Assert.IsTrue(c1);
                var r1 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(ciid1, rr1.Relation.FromCIID);
                Assert.AreEqual(ciid2, rr1.Relation.ToCIID);
                Assert.AreEqual(layerID1, rr1.LayerID);
                Assert.AreEqual(RelationState.New, rr1.Relation.State);
                Assert.AreEqual((await changeset.GetChangeset(trans)).ID, rr1.Relation.ChangesetID);

                // test repeated insertion
                var (i2, c2) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                Assert.IsFalse(c2);
                r1 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                rr1 = r1.First();
                Assert.AreEqual(RelationState.New, rr1.Relation.State); // state must still be New


                // test second relation
                var (i3, c3) = await relationModel.InsertRelation(ciid1, ciid3, predicate1.ID, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicate1.ID, i3.PredicateID);
                var r2 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, r2.Count());
                var rr2 = r2.FirstOrDefault(r => r.Relation.ToCIID == ciid3);
                Assert.AreEqual(ciid1, rr2.Relation.FromCIID);
                Assert.IsNotNull(rr2);
                Assert.AreEqual(layerID1, rr2.LayerID);
                Assert.AreEqual(RelationState.New, rr2.Relation.State);
                Assert.AreEqual((await changeset.GetChangeset(trans)).ID, rr2.Relation.ChangesetID);
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.ThrowsAsync<Exception>(async () => await relationModel.InsertRelation(ciid1, ciid3, "unknown predicate ID", layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans));
            }
        }

        [Test]
        public async Task TestMerging()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var layerModel = new LayerModel();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layer2 = await layerModel.CreateLayer("l2", trans);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });

            var (i1, c1) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var (i2, c2) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            Assert.AreEqual(predicate1.ID, i1.PredicateID);
            Assert.AreEqual(predicate1.ID, i2.PredicateID);
            Assert.IsTrue(c1);
            Assert.IsTrue(c2);
            var r1 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layer2.ID, rr1.LayerID);
        }


        [Test]
        public async Task TestGetRelationsByPredicate()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var layerModel = new LayerModel();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);
            var (predicateID1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var (predicateID2, changedP2) = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i2 = await relationModel.InsertRelation(ciid1, ciid2, predicateID2.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i3 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i4 = await relationModel.InsertRelation(ciid3, ciid1, predicateID2.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var r1 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID1.ID), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r1.Count());
            Assert.IsFalse(r1.Any(r => r.Relation.PredicateID == predicateID2.ID));
            var r2 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID2.ID), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r2.Count());
            Assert.IsFalse(r2.Any(r => r.Relation.PredicateID == predicateID1.ID));
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var layer1 = await layerModel.CreateLayer("l1", transI);
            var layer2 = await layerModel.CreateLayer("l2", transI);
            var layerset = new LayerSet(new long[] { layer2.ID, layer1.ID });
            var ciid1 = await ciModel.CreateCI(transI);
            var ciid2 = await ciModel.CreateCI(transI);
            var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, transI);

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var (i1, c1) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (i2, c2) = await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicate1.ID, i1.PredicateID);
                Assert.AreEqual(predicate1.ID, i2.PredicateID);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var removedRelation = await relationModel.RemoveRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, trans);
                Assert.IsNotNull(removedRelation);
                var r1 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(layer1.ID, rr1.LayerID);
                trans.Commit();
            }

            // add relation again
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var r2 = await relationModel.GetMergedRelations(new RelationSelectionFrom(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r2.Count());
                var rr2 = r2.First();
                Assert.AreEqual(layer2.ID, rr2.LayerID);
                trans.Commit();
            }
        }


        [Test]
        public async Task TestBulkReplace()
        {
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);
            var (predicateID1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer1 = await layerModel.CreateLayer("l1", trans);
            var layerset = new LayerSet(new long[] { layer1.ID });

            var i1 = await relationModel.InsertRelation(ciid1, ciid2, predicateID1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i2 = await relationModel.InsertRelation(ciid2, ciid3, predicateID1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i3 = await relationModel.InsertRelation(ciid1, ciid3, predicateID1.ID, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            trans.Commit();

            // test bulk replace
            using var trans2 = ModelContextBuilder.BuildDeferred();
            var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await relationModel.BulkReplaceRelations(new BulkRelationDataPredicateScope(predicateID1.ID, layer1.ID, new BulkRelationDataPredicateScope.Fragment[] {
                    new BulkRelationDataPredicateScope.Fragment(ciid1, ciid2),
                    new BulkRelationDataPredicateScope.Fragment(ciid2, ciid1),
                    new BulkRelationDataPredicateScope.Fragment(ciid3, ciid2),
                    new BulkRelationDataPredicateScope.Fragment(ciid3, ciid1)
                }), changeset2, new DataOriginV1(DataOriginType.Manual), trans2);

            var r1 = await relationModel.GetMergedRelations(new RelationSelectionWithPredicate(predicateID1.ID), layerset, trans2, TimeThreshold.BuildLatest());
            Assert.AreEqual(4, r1.Count());
        }
    }
}
