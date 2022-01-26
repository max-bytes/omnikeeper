using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Omnikeeper.Base.Model.IChangesetModel;

namespace Tests.Integration.Model
{
    class ChangesetModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            using var trans1 = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans1);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans1);
            var ciid3 = await GetService<ICIModel>().CreateCI(trans1);
            trans1.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans2);
            var layerset = new LayerSet(new string[] { layer1.ID });
            var changeset1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changeset3 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t2, layerset, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t3, layerset, ChangesetSelectionSpecificCIs.Build(ciid3), transI);
            Assert.AreEqual(2, changesets2.Count());

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }
            var t4 = DateTimeOffset.Now;

            var changesets3 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(3, changesets3.Count());
            var changesets4 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), transI, 2);
            Assert.AreEqual(2, changesets4.Count());
            var changesets5 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset, ChangesetSelectionSpecificCIs.Build(ciid2), transI, 1);
            Assert.AreEqual(1, changesets5.Count());

            var changesets6 = await GetService<IChangesetModel>().GetChangesets(changesets4.Select(c => c.ID).ToHashSet(), transI);
            Assert.AreEqual(2, changesets6.Count());
        }



        [Test]
        public async Task TestRelations()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var predicateID1 = "predicate_1";
            var predicateID2 = "predicate_2";
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans2);
            var layerset = new LayerSet(new string[] { layer1.ID });
            var changeset1 = await CreateChangesetProxy();
            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy();
            await GetService<IRelationModel>().InsertRelation(ciid2, ciid1, predicateID2, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets1 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t2, layerset, ChangesetSelectionSpecificCIs.Build(ciid1), transI);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t3, layerset, ChangesetSelectionSpecificCIs.Build(ciid1), transI);
            Assert.AreEqual(2, changesets2.Count());
        }

        [Test]
        public async Task DeleteEmptyTest()
        {
            using var trans1 = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans1);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans1);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans1);
            var (layer2, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l2", trans1);
            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var changeset1 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(100)));
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans1);
            trans1.Commit();

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(0, await GetService<IChangesetModel>().DeleteEmptyChangesets(trans));
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IBaseAttributeRevisionistModel>().DeleteAllAttributes(layer1.ID, trans);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(1, await GetService<IChangesetModel>().DeleteEmptyChangesets(trans));
                trans.Commit();
            }
        }


        [Test]
        [Obsolete]
        public async Task ArchiveOldTest()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var predicateID1 = "predicate_1";
            trans.Commit();

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans2);
            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var changeset1 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(100)));
            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans2);
            trans2.Commit();

            using var transI = ModelContextBuilder.BuildImmediate();
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));


            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(150)));
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("bar"), ciid1, layer1.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans3);
            trans3.Commit();

            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));

            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(100), transI));

            // changeset1 is now old "enough", but still cannot be deleted because its relation is the latest
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // delete relation again
            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changeset3 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)));
            await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans4, MaskHandlingForRemovalApplyNoMask.Instance);
            trans4.Commit();

            // changeset1 is now old "enough", and can be deleted
            Assert.AreEqual(1, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // overwrite attribute a1
            using var trans5 = ModelContextBuilder.BuildDeferred();
            var changeset4 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)));
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("new foo"), ciid1, layer1.ID, changeset4, new DataOriginV1(DataOriginType.Manual), trans5);
            trans5.Commit();

            // changeset2 is now old "enough", but still cannot be deleted because one of its attributes (a2) is the latest
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // delete attribute a2
            using var trans6 = ModelContextBuilder.BuildDeferred();
            var changeset5 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(250)));
            await GetService<IAttributeModel>().RemoveAttribute("a2", ciid1, layer1.ID, changeset5, new DataOriginV1(DataOriginType.Manual), trans6, MaskHandlingForRemovalApplyNoMask.Instance);
            trans6.Commit();

            // changeset2 is now old "enough", and can be deleted
            Assert.AreEqual(1, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // other changeset can be deleted, if threshold is large enough
            Assert.AreEqual(2, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(9999), transI));

            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset1.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset2.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset3.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNotNull(await GetService<IChangesetModel>().GetChangeset((await changeset4.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset5.GetChangeset(layer1.ID, new DataOriginV1(DataOriginType.Manual), transI)).ID, transI));
        }
    }
}
