using LandscapePrototype;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
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

namespace Tests.Integration
{
    class DBStressTest
    {
        private List<string> ciNames;

        [SetUp]
        public void Setup()
        {
            //var timer = new Stopwatch();
            //timer.Start();

            TestDBSetup.Setup();

            var model = new CIModel();

            var random = new Random(3);

            var numCIs = 5000;
            var numLayers = 2;
            var numAttributeInserts = 60000;
            var numAttributeNames = 50;

            ciNames = Enumerable.Range(0, numCIs).Select(i =>
            {
                var identity = "CI" + RandomString.Generate(8);
                return identity;
            }).ToList();

            using (var conn = model.CreateOpenConnection(TestDBSetup.dbName))
            {
                var changesetID = model.CreateChangeset(conn);

                Console.WriteLine(ciNames.Count());
                var cis = ciNames.Select(identity =>
                {
                    return (model.CreateCI(identity, conn), identity);
                }).ToList();

                var layerIDs = Enumerable.Range(0, numLayers).Select(i =>
                {
                    var identity = "L" + RandomString.Generate(8);
                    return model.CreateLayer(identity, conn);
                }).ToList();

                var attributeNames = Enumerable.Range(0, numAttributeNames).Select(i => "A" + RandomString.Generate(32)).ToList();
                var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
                {
                    var name = attributeNames.GetRandom(random);
                    var value = AttributeValueText.Build("V" + RandomString.Generate(8));
                    var layer = layerIDs.GetRandom(random);
                    var ci = cis.GetRandom(random).identity;
                    return model.InsertAttribute(name, value, layer, ci, changesetID, conn);
                }).ToList();

            }
            //timer.Stop();
            //Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public void TestSelectOnBigDatabase()
        {
            var model = new CIModel();
            using (var conn = model.CreateOpenConnection(TestDBSetup.dbName))
            {
                var timer = new Stopwatch();
                timer.Start();
                foreach (var ciName in ciNames)
                {
                    var a1 = model.GetMergedAttributes(ciName, false, conn);

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
}
