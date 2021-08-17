using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class CIModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestGetCIs()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var model = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var layerModel = new LayerModel();
            var user = await DBSetup.SetupUser(userModel, ModelContextBuilder.BuildImmediate());
            Guid ciid1;
            Guid ciid2;
            Guid ciid3;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                ciid1 = await model.CreateCI(trans);
                ciid2 = await model.CreateCI(trans);
                ciid3 = await model.CreateCI(trans);
                trans.Commit();
            }

            string layerID1;
            string layerID2;
            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var layer1 = await layerModel.UpsertLayer("l1", trans);
                var layer2 = await layerModel.UpsertLayer("l2", trans);
                layerID1 = layer1.ID;
                layerID2 = layer2.ID;
                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                var i1 = await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("text1"), ciid1, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i2 = await attributeModel.InsertAttribute("a2", new AttributeScalarValueText("text1"), ciid2, layerID1, changeset, new DataOriginV1(DataOriginType.Manual), trans);
                var i3 = await attributeModel.InsertAttribute("a3", new AttributeScalarValueText("text1"), ciid1, layerID2, changeset, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var cis1 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID1), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, cis1.Count());
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a1")));
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid2 && c.MergedAttributes.ContainsKey("a2")));
                var cis2 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), false, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, cis2.Count());
                Assert.AreEqual(1, cis2.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                var cis3 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), true, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(3, cis3.Count());
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid2 && c.MergedAttributes.Count() == 0));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid3 && c.MergedAttributes.Count() == 0));

                trans.Commit();
            }
        }

        [Test]
        public async Task TestLayerSets()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            using var trans = ModelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);

            var ciid1 = await model.CreateCI(trans);
            var layer1 = await layerModel.UpsertLayer("l1", trans);
            var layer2 = await layerModel.UpsertLayer("l2", trans);

            var layerset1 = new LayerSet(new string[] { layer1.ID });
            var layerset2 = new LayerSet(new string[] { layer2.ID });
            var layerset3 = new LayerSet(new string[] { layer1.ID, layer2.ID });
            var layerset4 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);
            await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);

            var a2 = await attributeModel.GetMergedAttributes(ciid1, layerset2, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a2.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a2.First().Value.Attribute.Value);

            var a3 = await attributeModel.GetMergedAttributes(ciid1, layerset3, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a3.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a3.First().Value.Attribute.Value);

            var a4 = await attributeModel.GetMergedAttributes(ciid1, layerset4, trans, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a4.Count());
            Assert.AreEqual(new AttributeScalarValueText("textL2"), a4.First().Value.Attribute.Value);
        }

        [Test]
        public async Task TestRemoveShowsLayerBelow()
        {
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel()));
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            var model = new CIModel(attributeModel, new CIIDModel());
            var layerModel = new LayerModel();
            var transI = ModelContextBuilder.BuildImmediate();
            var user = await DBSetup.SetupUser(userModel, transI);

            var ciid1 = await model.CreateCI(transI);
            var layer1 = await layerModel.UpsertLayer("l1", transI);
            var layer2 = await layerModel.UpsertLayer("l2", transI);
            var layerset1 = new LayerSet(new string[] { layer2.ID, layer1.ID });

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset1 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL1"), ciid1, layer1.ID, changeset1, new DataOriginV1(DataOriginType.Manual), trans);

                var changeset2 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.InsertAttribute("a1", new AttributeScalarValueText("textL2"), ciid1, layer2.ID, changeset2, new DataOriginV1(DataOriginType.Manual), trans);

                trans.Commit();
            }

            using (var trans = ModelContextBuilder.BuildDeferred())
            {
                var changeset3 = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);
                await attributeModel.RemoveAttribute("a1", ciid1, layer2.ID, changeset3, new DataOriginV1(DataOriginType.Manual), trans);
                trans.Commit();
            }

            var a1 = await attributeModel.GetMergedAttributes(ciid1, layerset1, transI, TimeThreshold.BuildLatest());
            Assert.AreEqual(1, a1.Count()); // layerID1 shines through deleted
            Assert.AreEqual(new AttributeScalarValueText("textL1"), a1.First().Value.Attribute.Value);
        }
    }
}
