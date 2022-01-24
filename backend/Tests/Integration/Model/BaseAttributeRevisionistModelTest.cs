using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class BaseAttributeRevisionistModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestArchiveOutdatedAttributesOlderThan()
        {
            var model = new BaseAttributeRevisionistModel();

            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await ciModel.CreateCI(trans);
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
            var d1 = await model.DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d1);

            // insert attributes
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // nothing to delete yet still
            var d2 = await model.DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d2);

            // override attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text2"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            // nothing to delete yet still, if we choose an older time threshold
            var d3 = await model.DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now.AddSeconds(-100), TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d3);

            // outdated attribute will be deleted, if we choose a time threshold that is recent enough
            var d4 = await model.DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, d4);

            // nothing to delete again
            var d5 = await model.DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d5);
        }
    }
}
