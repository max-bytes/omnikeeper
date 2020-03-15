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
            var userModel = new UserModel(conn);
            var user = await DBSetup.SetupUser(userModel);

            using (var trans = conn.BeginTransaction())
            {
                var changesetModel = new ChangesetModel(userModel, conn);
                var model = new CIModel(conn);
                var layerModel = new LayerModel(conn);

                var numCIs = 500;
                var numLayers = 2;
                var numAttributeInserts = 6000;
                var numAttributeNames = 50;

                var random = new Random(3);

                ciNames = Enumerable.Range(0, numCIs).Select(i =>
                {
                    var identity = "CI" + RandomString.Generate(8, random);
                    return identity;
                }).ToList();

                layerNames = Enumerable.Range(0, numLayers).Select(i =>
                {
                    var identity = "L" + RandomString.Generate(8, random);
                    return identity;
                }).ToList();

                var changeset = await changesetModel.CreateChangeset(user.ID, trans);


                //Console.WriteLine(ciNames.Count());
                var cis = ciNames.Select(identity =>
                {
                    return (model.CreateCI(identity, trans).GetAwaiter().GetResult(), identity);
                }).ToList();

                var layerIDs = layerNames.Select(identity =>
                {
                    return layerModel.CreateLayer(identity, trans).GetAwaiter().GetResult();
                }).ToList();

                var attributeNames = Enumerable.Range(0, numAttributeNames).Select(i => "A" + RandomString.Generate(32, random)).ToList();
                var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
                {
                    var name = attributeNames.GetRandom(random);
                    var value = AttributeValueText.Build("V" + RandomString.Generate(8, random));
                    var layer = layerIDs.GetRandom(random);
                    var ciid = cis.GetRandom(random).Item1;
                    return model.InsertAttribute(name, value, layer, ciid, changeset.ID, trans).GetAwaiter().GetResult();
                }).ToList();

                await trans.CommitAsync();

                timer.Stop();
                Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");

            }
        }

        [Test]
        public void TestSelectOnBigDatabase()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            using var trans = conn.BeginTransaction();
            var model = new CIModel(conn);
            var layerModel = new LayerModel(conn);

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray(), trans).GetAwaiter().GetResult();

            var timer = new Stopwatch();
            timer.Start();
            foreach (var ciName in ciNames)
            {
                var a1 = model.GetMergedAttributes(ciName, false, layerset, trans, DateTimeOffset.Now);

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
