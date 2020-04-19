using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Cached;
using LandscapeRegistry.Utils;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var predicateModel = new CachedPredicateModel(new PredicateModel(conn));
            var relationModel = new RelationModel(predicateModel, conn);
            var layerModel = new LayerModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            var ciid1 = await ciModel.CreateCI(null);
            var ciid2 = await ciModel.CreateCI(null);
            var ciid3 = await ciModel.CreateCI(null);

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layer1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layer1.ID });
            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans2);
            await attributeModel.InsertAttribute("a1", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid2, changeset1.ID, trans2);
            trans2.Commit();

            Thread.Sleep(500);

            using var trans3 = conn.BeginTransaction();
            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans3);
            await attributeModel.InsertAttribute("a2", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid3, changeset2.ID, trans3);
            trans3.Commit();

            var t2 = DateTimeOffset.Now;

            using var trans4 = conn.BeginTransaction();
            var changeset3 = await changesetModel.CreateChangeset(user.ID, trans4);
            await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid3, changeset3.ID, trans4);
            trans4.Commit();

            var t3 = DateTimeOffset.Now;

            var changesets = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, IncludeRelationDirections.Forward, null);
            Assert.AreEqual(2, changesets.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, IncludeRelationDirections.Forward, ciid3, null);
            Assert.AreEqual(2, changesets2.Count());


            using (var trans = conn.BeginTransaction())
            {
                var changeset = await changesetModel.CreateChangeset(user.ID, trans);
                await attributeModel.InsertAttribute("a3", AttributeValueTextScalar.Build("textL1"), layer1.ID, ciid2, changeset3.ID, trans);
                trans.Commit();
            }
            var t4 = DateTimeOffset.Now;

            var changesets3 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, IncludeRelationDirections.Forward, null);
            Assert.AreEqual(3, changesets3.Count());
            var changesets4 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, IncludeRelationDirections.Forward, null, 2);
            Assert.AreEqual(2, changesets4.Count());
            var changesets5 = await changesetModel.GetChangesetsInTimespan(t1, t4, layerset, IncludeRelationDirections.Forward, ciid2, null, 1);
            Assert.AreEqual(1, changesets5.Count());
        }



        [Test]
        public async Task TestRelations()
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
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var predicate1 = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, trans);
            var predicate2 = await predicateModel.InsertOrUpdate("predicate_2", "", "", AnchorState.Active, trans);
            trans.Commit();

            var t1 = DateTimeOffset.Now;

            using var trans2 = conn.BeginTransaction();
            var layer1 = await layerModel.CreateLayer("l1", trans2);
            var layerset = new LayerSet(new long[] { layer1.ID });
            var changeset1 = await changesetModel.CreateChangeset(user.ID, trans2);
            await relationModel.InsertRelation(ciid1, ciid2, predicate1.ID, layer1.ID, changeset1.ID, trans2);
            trans2.Commit();

            Thread.Sleep(500);
            var t2 = DateTimeOffset.Now;

            using var trans3 = conn.BeginTransaction();
            var changeset2 = await changesetModel.CreateChangeset(user.ID, trans3);
            await relationModel.InsertRelation(ciid2, ciid1, predicate2.ID, layer1.ID, changeset2.ID, trans3);
            trans3.Commit();

            Thread.Sleep(500);
            var t3 = DateTimeOffset.Now;

            var changesets1 = await changesetModel.GetChangesetsInTimespan(t1, t2, layerset, IncludeRelationDirections.Forward, ciid1, null);
            Assert.AreEqual(1, changesets1.Count());

            var changesets2 = await changesetModel.GetChangesetsInTimespan(t1, t3, layerset, IncludeRelationDirections.Forward, ciid1, null);
            Assert.AreEqual(1, changesets2.Count()); // must still be 1, as incoming relations are not counted
        }
    }
}
