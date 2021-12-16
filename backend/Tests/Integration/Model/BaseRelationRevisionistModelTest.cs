using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class BaseRelationRevisionistModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestArchiveOutdatedRelationsOlderThan()
        {
            var model = new BaseRelationRevisionistModel();

            var partitionModel = new PartitionModel();
            var ciidModel = new CIIDModel();
            var baseAttributeModel = new BaseAttributeModel(partitionModel, ciidModel);
            var relationModel = new RelationModel(new BaseRelationModel(partitionModel));
            var attributeModel = new AttributeModel(baseAttributeModel);
            var ciModel = new CIModel(attributeModel, ciidModel);
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);
            Guid ciid1;
            Guid ciid2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
                ciid2 = await ciModel.CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var (layer1, _) = await layerModel.CreateLayerIfNotExists("l1", trans);
                layerID1 = layer1.ID;
                trans.Commit();
            }

            var layerIDs = new string[] { layerID1 };

            // nothing to delete yet
            var d1 = await model.DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d1);

            // insert relations
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, "p1", layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await relationModel.InsertRelation(ciid1, ciid2, "p2", layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // nothing to delete yet still
            var d2 = await model.DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d2);

            // override relation by deleting, then adding again
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await relationModel.RemoveRelation(ciid1, ciid2, "p1", layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await relationModel.InsertRelation(ciid1, ciid2, "p1", layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // nothing to delete yet still, if we choose an older time threshold
            var d3 = await model.DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now.AddSeconds(-100), TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d3);

            // outdated relations will be deleted, if we choose a time threshold that is recent enough
            var d4 = await model.DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(2, d4);

            // nothing to delete again
            var d5 = await model.DeleteOutdatedRelationsOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d5);
        }
    }
}
