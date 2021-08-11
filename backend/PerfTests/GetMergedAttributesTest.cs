using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;

namespace PerfTests
{
    class GetMergedAttributesTest : Base
    {
        private List<Guid> ciNames = new List<Guid>();
        private List<string> layerNames = new List<string>();
        private IModelContextBuilder? modelContextBuilder;

        [SetUp]
        public void Setup()
        {
            Setup(true);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        public async Task SetupData()
        {
            var timer = new Stopwatch();
            timer.Start();

            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());

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
                var identity = "L" + RandomUtility.GenerateRandomString(8, random);
                return identity;
            }).ToList();

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            //Console.WriteLine(ciNames.Count());
            using var mc = modelContextBuilder.BuildDeferred();
            var cis = ciNames.Select(identity =>
            {
                return (ciModel.CreateCI(identity, mc).GetAwaiter().GetResult(), identity);
            }).ToList();

            var layers = layerNames.Select(identity =>
            {
                return layerModel.UpsertLayer(identity, mc).GetAwaiter().GetResult();
            }).ToList();

            var attributeNames = Enumerable.Range(0, numAttributeNames).Select(i => "A" + RandomUtility.GenerateRandomString(32, random)).ToList();
            var attributes = Enumerable.Range(0, numAttributeInserts).Select(i =>
            {
                var name = attributeNames.GetRandom(random);
                var value = new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random));
                var layer = layers.GetRandom(random);
                var ciid = cis.GetRandom(random).Item1;
                return attributeModel.InsertAttribute(name!, value, ciid, layer!.ID, changeset, new DataOriginV1(DataOriginType.Manual), mc).GetAwaiter().GetResult();
            }).ToList();

            mc.Commit();

            timer.Stop();
            Console.WriteLine($"Setup - Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public async Task TestSelectOnBigDatabase()
        {
            await SetupData();

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();

            using var mc = modelContextBuilder!.BuildDeferred();

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();

            var timer = new Stopwatch();
            timer.Start();
            foreach (var ciName in ciNames)
            {
                var a1 = await attributeModel.GetMergedAttributes(ciName, layerset, mc, TimeThreshold.BuildLatest());

                Console.WriteLine($"{ciName} count: {a1.Count()}");
                //foreach (var aa in a1)
                //{
                //    Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Value.Value2String()} ");
                //}
            }
            timer.Stop();
            Console.WriteLine($"Elapsed time: {timer.ElapsedMilliseconds / 1000f}");

        }
    }
}
