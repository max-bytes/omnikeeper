using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using static Landscape.Base.Model.IChangesetModel;
using static Landscape.Base.Model.IRelationModel;

namespace Tests.Integration.Model
{
    class ChangesetModelTest
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
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var ciid3 = await ciModel.CreateCI(null);

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layer1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layer1.ID });
            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("textL1"), ciid2, layer1.ID, changeset1, trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = conn.BeginTransaction();
            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("textL1"), ciid3, layer1.ID, changeset2, trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = conn.BeginTransaction();
            var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("textL1"), ciid3, layer1.ID, changeset3, trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            var changesets = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, new ChangesetSelectionAllCIs(), null);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, new ChangesetSelectionSingleCI(ciid3), null);
            Assert.AreEqual(2, changesets2.Count());


            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a3", AttributeScalarValueText.Build("textL1"), ciid2, layer1.ID, changeset3, trans);
                trans.Commit();
            }
            var t4 = DateTimeOffset.Now;

            var changesets3 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), null);
            Assert.AreEqual(3, changesets3.Count());
            var changesets4 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionAllCIs(), null, 2);
            Assert.AreEqual(2, changesets4.Count());
            var changesets5 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, new ChangesetSelectionSingleCI(ciid2), null, 1);
            Assert.AreEqual(1, changesets5.Count());
        }



        [Test]
        public async Task TestRelations()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var (predicate1, changedP1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var (predicate2, changedP2) = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layer1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layer1.ID });
            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset1, trans2);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = conn.BeginTransaction();
            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            await relationModel.InsertRelation(ciid2, ciid1, predicate2.ID, layer1.ID, changeset2, trans3);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            var changesets1 = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, new ChangesetSelectionSingleCI(ciid1), null);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, new ChangesetSelectionSingleCI(ciid1), null);
            Assert.AreEqual(2, changesets2.Count());
        }


        [Test]
        public async Task ArchiveOldTest()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var baseAttributeModel = new BaseAttributeModel(conn);
            var attributeModel = new AttributeModel(baseAttributeModel);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var baseRelationModel = new BaseRelationModel(predicateModel, conn);
            var relationModel = new RelationModel(baseRelationModel);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var (predicate1, changedp1) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var (predicate2, changedp2) = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            trans.Commit();

            using var trans2 = conn.BeginTransaction();
            var layer1 = await layerModel.CreateLayer("l1", trans2);
            var layerset1 = new LayerSet(new long[] { layer1.ID });
            var changeset1 = ChangesetProxy.Build(user, DateTimeOffset.FromUnixTimeSeconds(100), changesetModel);
            await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset1, trans2);
            trans2.Commit();

            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), null));


            using var trans3 = conn.BeginTransaction();
            var changeset2 = ChangesetProxy.Build(user, DateTimeOffset.FromUnixTimeSeconds(150), changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("foo"), ciid1, layer1.ID, changeset2, trans3);
            await attributeModel.InsertAttribute("a2", AttributeScalarValueText.Build("bar"), ciid1, layer1.ID, changeset2, trans3);
            trans3.Commit();

            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(50), null));

            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(100), null));

            // changeset1 is now old "enough", but still cannot be deleted because its relation is the latest
            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), null));

            // delete relation again
            using var trans4 = conn.BeginTransaction();
            var changeset3 = ChangesetProxy.Build(user, DateTimeOffset.FromUnixTimeSeconds(200), changesetModel);
            await relationModel.RemoveRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset3, trans4);
            trans4.Commit();

            // changeset1 is now old "enough", and can be deleted
            Assert.AreEqual(1, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(101), null));

            // overwrite attribute a1
            using var trans5 = conn.BeginTransaction();
            var changeset4 = ChangesetProxy.Build(user, DateTimeOffset.FromUnixTimeSeconds(200), changesetModel);
            await attributeModel.InsertAttribute("a1", AttributeScalarValueText.Build("new foo"), ciid1, layer1.ID, changeset4, trans5);
            trans5.Commit();

            // changeset2 is now old "enough", but still cannot be deleted because one of its attributes (a2) is the latest
            Assert.AreEqual(0, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), null));


            // delete attribute a2
            using var trans6 = conn.BeginTransaction();
            var changeset5 = ChangesetProxy.Build(user, DateTimeOffset.FromUnixTimeSeconds(250), changesetModel);
            await attributeModel.RemoveAttribute("a2",ciid1, layer1.ID, changeset5, trans6);
            trans6.Commit();

            // changeset2 is now old "enough", and can be deleted
            Assert.AreEqual(1, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(151), null));


            // other changeset can be deleted, if threshold is large enough
            Assert.AreEqual(2, await changesetModel.ArchiveUnusedChangesetsOlderThan(DateTimeOffset.FromUnixTimeSeconds(9999), null));

            Assert.IsNull(await changesetModel.GetChangeset((await changeset1.GetChangeset(null)).ID, null));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset2.GetChangeset(null)).ID, null));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset3.GetChangeset(null)).ID, null));
            Assert.IsNotNull(await changesetModel.GetChangeset((await changeset4.GetChangeset(null)).ID, null));
            Assert.IsNull(await changesetModel.GetChangeset((await changeset5.GetChangeset(null)).ID, null));

        }
    }
}
