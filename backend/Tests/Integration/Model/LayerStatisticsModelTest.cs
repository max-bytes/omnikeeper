using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using LandscapeRegistry.Model.Decorators;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerStatisticsModelTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task RelationStatisticsTest()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var user = await DBSetup.SetupUser(userModel);
            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);

            var layerModel = new LayerModel(conn);
            var layerStatisticsModel = new LayerStatisticsModel(conn, layerModel);

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer = await layerModel.CreateLayer("test_layer", null);

            await relationModel.InsertRelation(ciid1, ciid2, predicate.ID, layer.ID, changeset, trans);

            var ch2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicate.ID, layer.ID, ch2, trans);

            await relationModel.InsertRelation(ciid1, ciid3, predicate.ID, layer.ID, changeset, trans);


            // active relation test
            var activeRelations = await layerStatisticsModel.GetActiveRelations(layer, trans);
            Assert.AreEqual(activeRelations, 1);

            // relation changes history
            var relationsChangesHistory = await layerStatisticsModel.GetRelationChangesHistory(layer, trans);
            Assert.AreEqual(relationsChangesHistory, 3);

        }

        [Test]
        public async Task GetLayerChangesetsHistoryTest()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);

            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);
            var predicateModel = new CachingPredicateModel(new PredicateModel(conn), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, conn));
            var attributeModel = new AttributeModel(new BaseAttributeModel(conn));
            var ciModel = new CIModel(attributeModel, conn);
            var user = await DBSetup.SetupUser(userModel);

            var layerModel = new LayerModel(conn);
            var layerStatisticsModel = new LayerStatisticsModel(conn, layerModel);

            var changeset = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            using var trans = conn.BeginTransaction();
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer = await layerModel.CreateLayer("test_layer", null);

            await relationModel.InsertRelation(ciid1, ciid2, predicate.ID, layer.ID, changeset, trans);
            await relationModel.InsertRelation(ciid1, ciid3, predicate.ID, layer.ID, changeset, trans);

            var ch2 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicate.ID, layer.ID, ch2, trans);

            var ch3 = ChangesetProxy.Build(user, DateTimeOffset.Now, changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid3, predicate.ID, layer.ID, ch3, trans);

            var layerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layer, trans);

            Assert.AreEqual(layerChangesetsHistory, 3);

        }
    }
}
