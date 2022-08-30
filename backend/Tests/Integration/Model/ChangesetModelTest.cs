using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
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
            var changesetProxy1 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changesetProxy1, trans2, OtherLayersValueHandlingForceWrite.Instance);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changesetProxy2 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changesetProxy2, trans3, OtherLayersValueHandlingForceWrite.Instance);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changesetProxy3 = await CreateChangesetProxy();
            await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid3, layer1.ID, changesetProxy3, trans4, OtherLayersValueHandlingForceWrite.Instance);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t2, layerset.LayerIDs, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t3, layerset.LayerIDs, ChangesetSelectionSpecificCIs.Build(ciid3), transI);
            Assert.AreEqual(2, changesets2.Count());

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL1"), ciid2, layer1.ID, changesetProxy3, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }
            var t4 = DateTimeOffset.Now;

            var changesets3 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset.LayerIDs, new ChangesetSelectionAllCIs(), transI);
            Assert.AreEqual(3, changesets3.Count());
            var changesets4 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset.LayerIDs, new ChangesetSelectionAllCIs(), transI, 2);
            Assert.AreEqual(2, changesets4.Count());
            var changesets5 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t4, layerset.LayerIDs, ChangesetSelectionSpecificCIs.Build(ciid2), transI, 1);
            Assert.AreEqual(1, changesets5.Count());

            var changesets6 = await GetService<IChangesetModel>().GetChangesets(changesets4.Select(c => c.ID).ToHashSet(), transI);
            Assert.AreEqual(2, changesets6.Count());

            // test latest
            var c1 = await GetService<IChangesetModel>().GetLatestChangeset(AllCIIDsSelection.Instance, NamedAttributesSelection.Build("a3"), null, new LayerSet(layer1.ID).LayerIDs, transI, TimeThreshold.BuildLatest());
            Assert.AreEqual(await changesetProxy3.GetChangeset(layer1.ID, transI), c1);

            // another insert
            var changesetProxy4 = await CreateChangesetProxy();
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IAttributeModel>().InsertAttribute("a3", new AttributeScalarValueText("textL2"), ciid3, layer1.ID, changesetProxy4, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // test latest again
            var c2 = await GetService<IChangesetModel>().GetLatestChangeset(AllCIIDsSelection.Instance, NamedAttributesSelection.Build("a3"), null, new LayerSet(layer1.ID).LayerIDs, transI, TimeThreshold.BuildLatest());
            Assert.AreEqual(await changesetProxy4.GetChangeset(layer1.ID, transI), c2);

            // test latest yet again, but with specific CIIDs
            var c3 = await GetService<IChangesetModel>().GetLatestChangeset(SpecificCIIDsSelection.Build(ciid2), NamedAttributesSelection.Build("a3"), null, new LayerSet(layer1.ID).LayerIDs, transI, TimeThreshold.BuildLatest());
            Assert.AreEqual(await changesetProxy3.GetChangeset(layer1.ID, transI), c3);

            // get empty latest changeset
            var c4 = await GetService<IChangesetModel>().GetLatestChangeset(AllCIIDsSelection.Instance, NamedAttributesSelection.Build("unused"), null, new LayerSet(layer1.ID).LayerIDs, transI, TimeThreshold.BuildLatest());
            Assert.IsNull(c4);
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
            var changeset1 = await CreateChangesetProxy();
            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset1, trans2, OtherLayersValueHandlingForceWrite.Instance);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy();
            await GetService<IRelationModel>().InsertRelation(ciid2, ciid1, predicateID2, false, layer1.ID, changeset2, trans3, OtherLayersValueHandlingForceWrite.Instance);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            using var transI = ModelContextBuilder.BuildImmediate();
            var changesets1 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t2, new string[] { layer1.ID }, ChangesetSelectionSpecificCIs.Build(ciid1), transI);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await GetService<IChangesetModel>().GetChangesetsInTimespan(t1, t3, new string[] { layer1.ID }, ChangesetSelectionSpecificCIs.Build(ciid1), transI);
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
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset1, trans1, OtherLayersValueHandlingForceWrite.Instance);
            trans1.Commit();

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(0, await GetService<IChangesetModel>().DeleteEmptyChangesets(1, trans));
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                await GetService<IBaseAttributeRevisionistModel>().DeleteAllAttributes(AllCIIDsSelection.Instance, layer1.ID, trans);
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                Assert.AreEqual(1, await GetService<IChangesetModel>().DeleteEmptyChangesets(2, trans));
                trans.Commit();
            }
        }

        [Test]
        public async Task UserOfChangesetTest()
        {
            using var trans1 = ModelContextBuilder.BuildDeferred();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans1);
            var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans1);
            var layerset = new LayerSet(new string[] { layer1.ID });
            trans1.Commit();

            var userModel = GetService<IUserInDatabaseModel>();
            var user1 = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate(), "user1", Guid.NewGuid(), UserType.Robot);
            var user2 = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate(), "user2", Guid.NewGuid(), UserType.Human);

            using var trans2 = ModelContextBuilder.BuildDeferred();
            var changesetProxy1 = CreateChangesetProxy(user1);
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changesetProxy1, trans2, OtherLayersValueHandlingForceWrite.Instance);
            var changeset1 = await changesetProxy1.GetChangeset(layer1.ID, trans2);
            trans2.Commit();

            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changesetProxy2 = CreateChangesetProxy(user2);
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changesetProxy2, trans3, OtherLayersValueHandlingForceWrite.Instance);
            var changeset2 = await changesetProxy2.GetChangeset(layer1.ID, trans3);
            trans3.Commit();

            using var trans4 = ModelContextBuilder.BuildImmediate();
            var c1 = await GetService<IChangesetModel>().GetChangeset(changeset1.ID, trans4);
            if (c1 == null)
            {
                Assert.Fail();
                return;
            }
            Assert.AreEqual(user1.ID, c1.UserID);
            var c2 = await GetService<IChangesetModel>().GetChangeset(changeset2.ID, trans4);
            if (c2 == null)
            {
                Assert.Fail();
                return;
            }
            Assert.AreEqual(user2.ID, c2.UserID);
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
            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, false, layer1.ID, changeset1, trans2, OtherLayersValueHandlingForceWrite.Instance);
            trans2.Commit();

            using var transI = ModelContextBuilder.BuildImmediate();
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));


            using var trans3 = ModelContextBuilder.BuildDeferred();
            var changeset2 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(150)));
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("foo"), ciid1, layer1.ID, changeset2, trans3, OtherLayersValueHandlingForceWrite.Instance);
            await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("bar"), ciid1, layer1.ID, changeset2, trans3, OtherLayersValueHandlingForceWrite.Instance);
            trans3.Commit();

            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), transI));

            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(100), transI));

            // changeset1 is now old "enough", but still cannot be deleted because its relation is the latest
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // delete relation again
            using var trans4 = ModelContextBuilder.BuildDeferred();
            var changeset3 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)));
            await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, predicateID1, layer1.ID, changeset3, trans4, MaskHandlingForRemovalApplyNoMask.Instance);
            trans4.Commit();

            // changeset1 is now old "enough", and can be deleted
            Assert.AreEqual(1, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), transI));

            // overwrite attribute a1
            using var trans5 = ModelContextBuilder.BuildDeferred();
            var changeset4 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(200)));
            await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("new foo"), ciid1, layer1.ID, changeset4, trans5, OtherLayersValueHandlingForceWrite.Instance);
            trans5.Commit();

            // changeset2 is now old "enough", but still cannot be deleted because one of its attributes (a2) is the latest
            Assert.AreEqual(0, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // delete attribute a2
            using var trans6 = ModelContextBuilder.BuildDeferred();
            var changeset5 = await CreateChangesetProxy(TimeThreshold.BuildAtTime(DateTimeOffset.FromUnixTimeSeconds(250)));
            await GetService<IAttributeModel>().RemoveAttribute("a2", ciid1, layer1.ID, changeset5, trans6, MaskHandlingForRemovalApplyNoMask.Instance);
            trans6.Commit();

            // changeset2 is now old "enough", and can be deleted
            Assert.AreEqual(1, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), transI));


            // other changeset can be deleted, if threshold is large enough
            Assert.AreEqual(2, await GetService<IChangesetModel>().ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(9999), transI));

            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset1.GetChangeset(layer1.ID, transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset2.GetChangeset(layer1.ID, transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset3.GetChangeset(layer1.ID, transI)).ID, transI));
            Assert.IsNotNull(await GetService<IChangesetModel>().GetChangeset((await changeset4.GetChangeset(layer1.ID, transI)).ID, transI));
            Assert.IsNull(await GetService<IChangesetModel>().GetChangeset((await changeset5.GetChangeset(layer1.ID, transI)).ID, transI));
        }
    }
}
