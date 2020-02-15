using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration
{
    class LayerModelTest
    {
        [SetUp]
        public void Setup()
        {
            TestDBSetup.Setup();
        }

        [Test]
        public async Task TestBasics()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(TestDBSetup.dbName, false, true);
            var layerModel = new LayerModel(conn);

            var layerNames = Enumerable.Range(0, 100).Select(i => $"l{i}");
            foreach (var ln in layerNames)
                await layerModel.CreateLayer(ln);

            //layerModel.CreateLayer("l1");
            //layerModel.CreateLayer("l2");
            //layerModel.CreateLayer("l3");

            //try
            //{
                var layerSet1 = await layerModel.BuildLayerSet(layerNames.ToArray());
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
