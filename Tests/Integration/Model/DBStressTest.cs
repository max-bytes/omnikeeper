using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using LandscapePrototype.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Model
{
    class DBStressTest
    {
        private List<string> ciNames;
        private List<string> layerNames;

        [SetUp]
        public async Task Setup()
        {
            var timer = new Stopwatch();
            timer.Start();

            DBSetup.Setup();
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);

            var random = new Random(3);

            var numCIs = 500;
            var numLayers = 2;
            var numAttributeInserts = 6000;
            var numAttributeNames = 50;

            ciNames = Enumerable.Range(0, numCIs).Select(i =>
            {
                var identity = "CI" + RandomString.Generate(8);
                return identity;
            }).ToList();

            layerNames = Enumerable.Range(0, numLayers).Select(i =>
            {
                var identity = "L" + RandomString.Generate(8);
                return identity;
            }).ToList();

            var changesetID = await model.CreateChangeset();

            Console.WriteLine(ciNames.Count());
            var cis = ciNames.Select(identity =>
            {
                return (model.CreateCI(identity).GetAwaiter().GetResult(), identity);
            }).ToList();

            var layerIDs = layerNames.Select(identity =>
            {
                return layerModel.CreateLayer(identity).GetAwaiter().GetResult();
            }).ToList();

            var attributeNames = Enumerable.Range(0, numAttributeNames).Select(i => "A" + RandomString.Generate(32)).ToList();
            var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var name = attributeNames.GetRandom(random);
                var value = AttributeValueText.Build("V" + RandomString.Generate(8));
                var layer = layerIDs.GetRandom(random);
                var ciid = cis.GetRandom(random).Item1;
                return model.InsertAttribute(name, value, layer, ciid, changesetID);
            }).ToList();

            timer.Stop();
            Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public void TestSelectOnBigDatabase()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray()).GetAwaiter().GetResult();

            var timer = new Stopwatch();
            timer.Start();
            foreach (var ciName in ciNames)
            {
                var a1 = model.GetMergedAttributes(ciName, false, layerset);

                //Console.WriteLine($"{ciName} count: {a1.Count()}");
                //foreach (var aa in a1)
                //{
                //    Console.WriteLine($"{aa.State} {aa.LayerID} {aa.Value} ");
                //}
            }
            timer.Stop();
            Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");

        }
    }
}
