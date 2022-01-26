using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class LayerModelTest : DIServicedTestBase
    {
        [Test]
        public async Task TestBasics()
        {
            using var trans = ModelContextBuilder.BuildDeferred();

            var layerNames = Enumerable.Range(0, 100).Select(i => $"l{i}");
            foreach (var ln in layerNames)
                await GetService<ILayerModel>().CreateLayerIfNotExists(ln, trans);

            //layerModel.CreateLayer("l1");
            //layerModel.CreateLayer("l2");
            //layerModel.CreateLayer("l3");

            //try
            //{
            var layerSet1 = await GetService<ILayerModel>().BuildLayerSet(layerNames.ToArray(), trans);
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
            using var trans = ModelContextBuilder.BuildImmediate();

            var (layerA, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("a", trans);
            var (layerB, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("b", trans);
            var (layerC, _) = await GetService<ILayerModel>().CreateLayerIfNotExists("c", trans);

            var user = await GetService<IUserInDatabaseModel>().UpsertUser("testuser", "testuser", Guid.NewGuid(), UserType.Human, trans);

            var ciid = await GetService<ICIModel>().CreateCI(trans);
            var changeset = await CreateChangesetProxy();

            await GetService<IAttributeModel>().InsertAttribute("attribute", new AttributeScalarValueText("foo"), ciid, layerC.ID, changeset, new DataOriginV1(DataOriginType.Manual), trans);

            Assert.AreEqual(true, await GetService<ILayerModel>().TryToDelete(layerA.ID, trans));
            Assert.AreEqual(true, await GetService<ILayerModel>().TryToDelete(layerB.ID, trans));
            Assert.AreEqual(false, await GetService<ILayerModel>().TryToDelete(layerC.ID, trans));
        }
    }
}
