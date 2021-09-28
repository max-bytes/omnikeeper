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
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
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
                var cis1 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID1), false, AllAttributeSelection.Instance, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(2, cis1.Count());
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a1")));
                Assert.AreEqual(1, cis1.Count(c => c.ID == ciid2 && c.MergedAttributes.ContainsKey("a2")));
                var cis2 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), false, AllAttributeSelection.Instance, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(1, cis2.Count());
                Assert.AreEqual(1, cis2.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                var cis3 = await model.GetMergedCIs(new AllCIIDsSelection(), new LayerSet(layerID2), true, AllAttributeSelection.Instance, trans, TimeThreshold.BuildLatest());
                Assert.AreEqual(3, cis3.Count());
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid1 && c.MergedAttributes.ContainsKey("a3")));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid2 && c.MergedAttributes.Count() == 0));
                Assert.AreEqual(1, cis3.Count(c => c.ID == ciid3 && c.MergedAttributes.Count() == 0));

                trans.Commit();
            }
        }

    }
}
