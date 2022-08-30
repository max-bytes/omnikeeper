using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class BaseRelationRevisionistModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestArchiveOutdatedRelationsOlderThan()
        {
            Guid ciid1;
            Guid ciid2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
                ciid2 = await GetService<ICIModel>().CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                trans.Commit();
            }

            var layerIDs = new string[] { layerID1 };

            // nothing to delete yet
            var d1 = await GetService<IBaseRelationRevisionistModel>().DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d1);

            // insert relations
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, "p1", false, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, "p2", false, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // nothing to delete yet still
            var d2 = await GetService<IBaseRelationRevisionistModel>().DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d2);

            // override relation by deleting, then adding again
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IRelationModel>().RemoveRelation(ciid1, ciid2, "p1", layerID1, changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IRelationModel>().InsertRelation(ciid1, ciid2, "p1", false, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // nothing to delete yet still, if we choose an older time threshold
            var d3 = await GetService<IBaseRelationRevisionistModel>().DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now.AddSeconds(-100), TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d3);

            // outdated relations will be deleted, if we choose a time threshold that is recent enough
            var d4 = await GetService<IBaseRelationRevisionistModel>().DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, d4);

            // nothing to delete again
            var d5 = await GetService<IBaseRelationRevisionistModel>().DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d5);
        }
    }
}
