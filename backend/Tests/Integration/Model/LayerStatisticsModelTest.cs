using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerStatisticsModelTest : DIServicedTestBase
    {
        [Test]
        public async Task RelationStatisticsTest()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var changeset = await CreateChangesetProxy();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);

            var predicateID1 = "predicate_1";

            //var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);

            var (layer, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("test_layer", trans);

            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = await CreateChangesetProxy();

            await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, predicateID1, layer.ID, ch2, new DataOriginV1(DataOriginType.Manual), trans);

            await GetService<IRelationModel>().InsertRelation(ciid1, ciid3, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);


            // active relation test
            var activeRelations = await GetService<ILayerStatisticsModel>().GetActiveRelations(layer.ID, trans);
            Assert.AreEqual(activeRelations, 1);

            // relation changes history
            var relationsChangesHistory = await GetService<ILayerStatisticsModel>().GetRelationChangesHistory(layer.ID, trans);
            Assert.AreEqual(relationsChangesHistory, 3);

        }

        [Test]
        public async Task GetLayerChangesetsHistoryTest()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var changeset = await CreateChangesetProxy();
            var ciid1 = await GetService<ICIModel>().CreateCI(trans);
            var ciid2 = await GetService<ICIModel>().CreateCI(trans);
            var ciid3 = await GetService<ICIModel>().CreateCI(trans);

            //var (predicate, changed) = await predicateModel.InsertOrUpdate("predicate_1", "", "", AnchorState.Active, PredicateModel.DefaultConstraits, trans);
            var predicateID1 = "predicate_1";

            var (layer, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("test_layer", trans);

            await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            await GetService<IRelationModel>().InsertRelation(ciid1, ciid3, predicateID1, layer.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var ch2 = await CreateChangesetProxy();

            await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, predicateID1, layer.ID, ch2, new DataOriginV1(DataOriginType.Manual), trans);

            var ch3 = await CreateChangesetProxy();

            await GetService<IRelationModel>().RemoveRelation(ciid1, ciid3, predicateID1, layer.ID, ch3, new DataOriginV1(DataOriginType.Manual), trans);

            var layerChangesetsHistory = await GetService<ILayerStatisticsModel>().GetLayerChangesetsHistory(layer.ID, trans);

            Assert.AreEqual(layerChangesetsHistory, 3);

        }
    }
}
