using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class BaseAttributeRevisionistModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestArchiveOutdatedAttributesOlderThan()
        {
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
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
            var d1 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d1);

            // insert attributes
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // nothing to delete yet still
            var d2 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d2);

            // override attribute
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text2"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // nothing to delete yet still, if we choose an older time threshold
            var d3 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now.AddSeconds(-100), TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d3);

            // outdated attribute will be deleted, if we choose a time threshold that is recent enough
            var d4 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, d4);

            // nothing to delete again
            var d5 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(0, d5);
        }


        [Test]
        public async Task TestDeleteOutdatedAttributesOlderThanForRemovedAttributes()
        {
            Guid ciid1;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await GetService<ICIModel>().CreateCI(trans);
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

            // insert attributes
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                await GetService<IAttributeModel>().InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, trans, OtherLayersValueHandlingForceWrite.Instance);
                trans.Commit();
            }

            // delete attributes again
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = await CreateChangesetProxy();
                await GetService<IAttributeModel>().RemoveAttribute("a1", ciid1, layerID1, changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                await GetService<IAttributeModel>().RemoveAttribute("a2", ciid1, layerID1, changeset, trans, MaskHandlingForRemovalApplyNoMask.Instance);
                trans.Commit();
            }

            // model should delete these removed attributes
            var d1 = await GetService<IBaseAttributeRevisionistModel>().DeleteOutdatedAttributesOlderThan(layerIDs, ModelContextBuilder.BuildImmediate(), DateTimeOffset.Now, TimeThreshold.BuildLatest());
            Assert.AreEqual(4, d1);

        }
    }
}
