using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Utils.Serialization;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerStatisticsModelTest // TODO: use some TestBase class
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
            using var conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            //var predicateModel = new CachingPredicateModel(new PredicateModel());
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());

            var layerModel = new LayerModel();
            var layerStatisticsModel = new LayerStatisticsModel();

            using var trans = modelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            var predicateID1 = "predicate_1";

            //var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var layer = await layerModel.UpsertLayer("test_layer", trans);

            await relationModel.InsertRelation(ciid1, ciid2, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicateID1, layer.ID, ch2, new DataOriginV1(DataOriginType.Manual), trans);

            await relationModel.InsertRelation(ciid1, ciid3, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);


            // active relation test
            var activeRelations = await layerStatisticsModel.GetActiveRelations(layer.ID, trans);
            Assert.AreEqual(activeRelations, 1);

            // relation changes history
            var relationsChangesHistory = await layerStatisticsModel.GetRelationChangesHistory(layer.ID, trans);
            Assert.AreEqual(relationsChangesHistory, 3);

        }

        [Test]
        public async Task GetLayerChangesetsHistoryTest()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.BuildFromUserSecrets(GetType().Assembly, true);
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var relationModel = new RelationModel(new BaseRelationModel(new PartitionModel()));
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());

            var layerModel = new LayerModel();
            var layerStatisticsModel = new LayerStatisticsModel();


            using var trans = modelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            var ciid1 = await ciModel.CreateCI(trans);
            var ciid2 = await ciModel.CreateCI(trans);
            var ciid3 = await ciModel.CreateCI(trans);

            //var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID1 = "predicate_1";

            var layer = await layerModel.UpsertLayer("test_layer", trans);

            await relationModel.InsertRelation(ciid1, ciid2, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            await relationModel.InsertRelation(ciid1, ciid3, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid2, predicateID1, layer.ID, ch2, new DataOriginV1(DataOriginType.Manual), trans);

            var ch3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await relationModel.RemoveRelation(ciid1, ciid3, predicateID1, layer.ID, ch3, new DataOriginV1(DataOriginType.Manual), trans);

            var layerChangesetsHistory = await layerStatisticsModel.GetLayerChangesetsHistory(layer.ID, trans);

            Assert.AreEqual(layerChangesetsHistory, 3);

        }
    }
}
