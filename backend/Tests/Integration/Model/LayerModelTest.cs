using Landscape.Base.Entity;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerModelTest
    {
        [SetUp]
        public void Setup()
        {
            DBSetup.Setup();
        }

        [Test]
        public async Task TestBasics()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var layerModel = new LayerModel(conn);

            var layerNames = Enumerable.Range(0, 100).Select(i => $"l{i}");
            foreach (var ln in layerNames)
                await layerModel.CreateLayer(ln, trans);

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
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var layerModel = new LayerModel(conn);
            var attributeModel = new AttributeModel(conn);
            var ciModel = new CIModel(attributeModel, conn);
            var userModel = new UserInDatabaseModel(conn);
            var changesetModel = new ChangesetModel(userModel, conn);

            var layerA = await layerModel.CreateLayer("a", null);
            var layerB = await layerModel.CreateLayer("b", AnchorState.Deprecated, ComputeLayerBrain.Build("clbB"), null);
            var layerC = await layerModel.CreateLayer("c", AnchorState.Deprecated, ComputeLayerBrain.Build("clbC"), null);

            var user = await userModel.UpsertUser("testuser", Guid.NewGuid(), UserType.Human, null);

            var ciid = await ciModel.CreateCI(null);
            var changeset = await changesetModel.CreateChangeset(user.ID, null);

            await attributeModel.InsertAttribute("attribute", AttributeValueTextScalar.Build("foo"), layerC.ID, ciid, changeset.ID, null);

            Assert.AreEqual(true, await layerModel.TryToDelete(layerA.ID, null));
            Assert.AreEqual(true, await layerModel.TryToDelete(layerB.ID, null));
            Assert.AreEqual(false, await layerModel.TryToDelete(layerC.ID, null));

        }
    }
}
