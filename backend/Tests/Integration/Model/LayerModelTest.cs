using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerModelTest : DBBackedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            using var trans = ModelContextBuilder.BuildDeferred();
            var layerModel = new LayerModel();

            var layerNames = Enumerable.Range(0, 100).Select(i => $"l{i}");
            foreach (var ln in layerNames)
                await layerModel.CreateLayerIfNotExists(ln, trans);

            //layerModel.CreateLayer("l1");
            //layerModel.CreateLayer("l2");
            //layerModel.CreateLayer("l3");

            //try
            //{
            var layerSet1 = await layerModel.BuildLayerSet(layerNames.ToArray(), trans);
            //var layerSet1 = await layerModel.BuildLayerSet(new string[] { "l1", "l2", "l3" });
            //Console.WriteLine($"mid");
            //var layerSet2 = await layerModel.BuildLayerSet(new string[] { "l2", "l3" });
            //} catch ( Exception e)
            //{
            //    Console.WriteLine(e);
            //}
        }

        [Test]
        public async Task TestDeletion()
        {
            var layerModel = new LayerModel();
            var attributeModel = new AttributeModel(new BaseAttributeModel(new PartitionModel(), new CIIDModel()));
            var ciModel = new CIModel(attributeModel, new CIIDModel());
            var userModel = new UserInDatabaseModel();
            var changesetModel = new ChangesetModel(userModel);
            using var trans = ModelContextBuilder.BuildImmediate();

            var (layerA, _) = await layerModel.CreateLayerIfNotExists("a", trans);
            var (layerB, _) = await layerModel.CreateLayerIfNotExists("b", trans);
            var (layerC, _) = await layerModel.CreateLayerIfNotExists("c", trans);

            var user = await userModel.UpsertUser("testuser", "testuser", Guid.NewGuid(), UserType.Human, trans);

            var ciid = await ciModel.CreateCI(trans);
            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            await attributeModel.InsertAttribute("attribute", new AttributeScalarValueText("foo"), ciid, layerC.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            Assert.AreEqual(true, await layerModel.TryToDelete(layerA.ID, trans));
            Assert.AreEqual(true, await layerModel.TryToDelete(layerB.ID, trans));
            Assert.AreEqual(false, await layerModel.TryToDelete(layerC.ID, trans));
        }
    }
}
