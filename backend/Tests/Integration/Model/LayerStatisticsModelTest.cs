using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
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
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel);

            var layerModel = new LayerModel();
            var layerStatisticsModel = new LayerStatisticsModel(layerModel);

            using var trans = modelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer = await layerModel.CreateLayer("test_layer", trans);

            await relationModel.InsertRelation(ciid1, ciid2, predicate.ID, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicate.ID, layer.ID, ch2, trans);

            await relationModel.InsertRelation(ciid1, ciid3, predicate.ID, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);


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
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(predicateModel, new PartitionModel()));
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var ciModel = new CIModel(attributeModel);

            var layerModel = new LayerModel();
            var layerStatisticsModel = new LayerStatisticsModel(layerModel);


            using var trans = modelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer = await layerModel.CreateLayer("test_layer", trans);

            await relationModel.InsertRelation(ciid1, ciid2, predicate.ID, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            await relationModel.InsertRelation(ciid1, ciid3, predicate.ID, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicate.ID, layer.ID, ch2, trans);

            var ch3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid3, predicate.ID, layer.ID, ch3, trans);

            var layerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layer, trans);

            Assert.AreEqual(layerChangesetsHistory, 3);

        }
    }
}
