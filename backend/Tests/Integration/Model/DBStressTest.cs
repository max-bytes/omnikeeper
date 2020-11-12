using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using Omnikeeper.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration.Model.Mocks;
using Omnikeeper.Base.Utils.ModelContext;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Integration.Model
{
    class DBStressTest
    {
        private List<Guid> ciNames = new List<Guid>();
        private List<string> layerNames = new List<string>();

        [SetUp]
        public async Task Setup()
        {
            var timer = new Stopwatch();
            timer.Start();

            DBSetup.Setup();
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var userModel = new UserInDatabaseModel();
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);

            using var trans = modelContextBuilder.BuildDeferred();
            var user = await DBSetup.SetupUser(userModel, trans);
            var changesetModel = new ChangesetModel(userModel);
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var model = new CIModel(attributeModel);
            var layerModel = new LayerModel();

            var numCIs = 500;
            var numLayers = 2;
            var numAttributeInserts = 6000;
            var numAttributeNames = 50;

            var random = new Random(3);

            ciNames = Enumerable.Range(0, numCIs).Select(i =>
            {
                    //var identity = "CI" + RandomString.Generate(8, random);
                    //return identity;
                    return Guid.NewGuid();
            }).ToList();

            layerNames = Enumerable.Range(0, numLayers).Select(i =>
            {
                var identity = "L" + RandomString.Generate(8, random);
                return identity;
            }).ToList();

            var changeset = new ChangesetProxy(user, DateTimeOffset.Now, changesetModel);


            //Console.WriteLine(ciNames.Count());
            var cis = ciNames.Select(identity =>
            {
                return (model.CreateCI(identity, trans).GetAwaiter().GetResult(), identity);
            }).ToList();

            var layers = layerNames.Select(identity =>
            {
                return layerModel.CreateLayer(identity, trans).GetAwaiter().GetResult();
            }).ToList();

            var attributeNames = Enumerable.Range(0, numAttributeNames).Select(i => "A" + RandomString.Generate(32, random)).ToList();
            var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var name = attributeNames.GetRandom(random);
                var value = new AttributeScalarValueText("V" + RandomString.Generate(8, random));
                var layer = layers.GetRandom(random);
                var ciid = cis.GetRandom(random).Item1;
                return attributeModel.InsertAttribute(name!, value, ciid, layer!.ID, changeset, trans).GetAwaiter().GetResult();
            }).ToList();

            trans.Commit();

            timer.Stop();
            Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public async Task TestSelectOnBigDatabase()
        {
            var dbcb = new DBConnectionBuilder();
            using var conn = dbcb.Build(DBSetup.dbName, false, true);
            var modelContextBuilder = new ModelContextBuilder(null, conn, NullLogger<IModelContext>.Instance);
            using var trans = modelContextBuilder.BuildDeferred();
            var attributeModel = new AttributeModel(new BaseAttributeModel());
            var model = new CIModel(attributeModel);
            var layerModel = new LayerModel();

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray(), trans).GetAwaiter().GetResult();

            var timer = new Stopwatch();
            timer.Start();
            foreach (var ciName in ciNames)
            {
                var a1 = await attributeModel.GetMergedAttributes(ciName, layerset, trans, TimeThreshold.BuildLatest());

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
