using LandscapeRegistry.Model;
using LandscapeRegistry.Utils;
using NUnit.Framework;
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
    }
}
