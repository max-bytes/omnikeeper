﻿using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class RelationModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            Guid ciid1;
            Guid ciid3;
            string layerID1;
            ChangesetProxy changeset;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                changeset = await CreateChangesetProxy();

                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                var ciid2 = await GetService<ICIModel>().CreateCI(trans);
                ciid3 = await GetService<ICIModel>().CreateCI(trans);
                var predicateID1 = "predicate1";

                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                var layerset = new LayerSet(new string[] { layerID1 });

                // test single relation
                var (i1, c1) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicateID1, i1.PredicateID);
                Assert.IsTrue(c1);
                var r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(ciid1, rr1.Relation.FromCIID);
                Assert.AreEqual(ciid2, rr1.Relation.ToCIID);
                Assert.AreEqual(layerID1, rr1.LayerStackIDs[0]);
                Assert.AreEqual((await changeset.GetChangeset(layerID1, new DataOriginV1(DataOriginType.Manual), trans)).ID, rr1.Relation.ChangesetID);

                // test repeated insertion
                var (i2, c2) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicateID1, i2.PredicateID);
                Assert.IsFalse(c2);
                r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());


                // test second relation
                var (i3, c3) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid3, predicateID1, false, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicateID1, i3.PredicateID);
                var r2 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, r2.Count());
                var rr2 = r2.FirstOrDefault(r => r.Relation.ToCIID == ciid3);
                Assert.AreEqual(ciid1, rr2.Relation.FromCIID);
                Assert.IsNotNull(rr2);
                Assert.AreEqual(layerID1, rr2.LayerStackIDs[0]);
                Assert.AreEqual((await changeset.GetChangeset(layerID1, new DataOriginV1(DataOriginType.Manual), trans)).ID, rr2.Relation.ChangesetID);

                trans.Commit();
            }
        }

        [Test]
        public async Task TestMerging()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var changeset = await CreateChangesetProxy();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            //var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID1 = "predicate1";

            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans);
            var layerset = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var (i1, c1) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var (i2, c2) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            Assert.AreEqual(predicateID1, i1.PredicateID);
            Assert.AreEqual(predicateID1, i2.PredicateID);
            Assert.IsTrue(c1);
            Assert.IsTrue(c2);
            var r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, r1.Count());
            var rr1 = r1.First();
            Assert.AreEqual(layer2.ID, rr1.LayerStackIDs[0]);
        }


        [Test]
        public async Task TestGetRelationsByPredicate()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var changeset = await CreateChangesetProxy();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);
            //var (predicateID1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            //var (predicateID2, changedP2) = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID1 = "predicate1";
            var predicateID2 = "predicate2";

            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var layerset = new LayerSet(new string[] { layer1.ID });

            var i1 = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i2 = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID2, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i3 = await GetService<IRelationModel>().InsertRelation(ciid2, ciid3, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i4 = await GetService<IRelationModel>().InsertRelation(ciid3, ciid1, predicateID2, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID1), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r1.Count());
            Assert.IsFalse(r1.Any(r => r.Relation.PredicateID == predicateID2));
            var r2 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID2), layerset, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, r2.Count());
            Assert.IsFalse(r2.Any(r => r.Relation.PredicateID == predicateID1));
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var transI = ModelContextBuilder.BuildImmediate();

            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", transI);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", transI);
            var layerset = new LayerSet(new string[] { layer2.ID, layer1.ID });
            var ciid1 = await GetService<ICIModel>().CreateCI(transI);
            var ciid2 = await GetService<ICIModel>().CreateCI(transI);
            //var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, transI);
            var predicateID1 = "predicate1";

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                var (i1, c1) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var (i2, c2) = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                Assert.AreEqual(predicateID1, i1.PredicateID);
                Assert.AreEqual(predicateID1, i2.PredicateID);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                var removedRelation = await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, predicateID1, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans, MaskHandlingForRemovalApplyNoMask.Instance);
                Assert.IsNotNull(removedRelation);
                var r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r1.Count());
                var rr1 = r1.First();
                Assert.AreEqual(layer1.ID, rr1.LayerStackIDs[0]);
                trans.Commit();
            }

            // add relation again
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var r2 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionFrom.Build(ciid1), layerset, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, r2.Count());
                var rr2 = r2.First();
                Assert.AreEqual(layer2.ID, rr2.LayerStackIDs[0]);
                trans.Commit();
            }
        }


        [Test]
        public async Task TestBulkReplace()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var changeset = await CreateChangesetProxy();

            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);
            //var (predicateID1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID1 = "predicate1";

            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
            var layerset = new LayerSet(new string[] { layer1.ID });

            var i1 = await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i2 = await GetService<IRelationModel>().InsertRelation(ciid2, ciid3, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            var i3 = await GetService<IRelationModel>().InsertRelation(ciid1, ciid3, predicateID1, false, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            trans.Commit();

            // test bulk replace
            using var trans2 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy();
            await GetService<IRelationModel>().BulkReplaceRelations(new BulkRelationDataPredicateScope(predicateID1, layer1.ID, new BulkRelationDataPredicateScope.Fragment[] {
                    new BulkRelationDataPredicateScope.Fragment(ciid1, ciid2, false),
                    new BulkRelationDataPredicateScope.Fragment(ciid2, ciid1, false),
                    new BulkRelationDataPredicateScope.Fragment(ciid3, ciid2, false),
                    new BulkRelationDataPredicateScope.Fragment(ciid3, ciid1, false)
                }), changeset2, new DataOriginV1(DataOriginType.Manual), trans2, MaskHandlingForRemovalApplyNoMask.Instance);

            var r1 = await GetService<IRelationModel>().GetMergedRelations(RelationSelectionWithPredicate.Build(predicateID1), layerset, trans2, TimeThreshold.BuildLatest());
            Assert.AreEqual(4, r1.Count());
            r1.Select(r => (r.Relation.PredicateID, r.Relation.FromCIID, r.Relation.ToCIID)).Should().BeEquivalentTo(new (string, Guid, Guid)[]
            {
                (predicateID1, ciid1, ciid2),
                (predicateID1, ciid2, ciid1),
                (predicateID1, ciid3, ciid2),
                (predicateID1, ciid3, ciid1),
            }, options => options.WithStrictOrdering());
        }
    }
}
