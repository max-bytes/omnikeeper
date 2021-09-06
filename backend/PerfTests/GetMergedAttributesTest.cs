using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests.Integration;

namespace PerfTests
{
    [Explicit]
    public class GetMergedAttributesTest : Base
    {
        private List<Guid> ciids = new List<Guid>();
        private List<string> layerNames = new List<string>();
        private IModelContextBuilder? modelContextBuilder;
        private IAttributeModel? attributeModel;
        private Omnikeeper.Base.Entity.LayerSet? layerset;
        private ICIIDSelection? specificCIIDs;
        private ICIIDSelection? allExceptCIIDs;
        private readonly Consumer consumer = new Consumer();

        [ParamsSource(nameof(AttributeCITuples))]
        public (int numCIs, int numAttributeInserts, int numLayers, int numAttributeNames) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numAttributeInserts, int numLayers, int numAttributeNames)> AttributeCITuples => new[] {
            (1000, 10000, 4, 50),
        };

        [Params("all", "specific", "allExcept")]
        public string? CIIDSelection { get; set; }

        [Params(false)]
        public bool WithModelCaching { get; set; }

        [Params(false)]
        public bool WithEffectiveTraitCaching { get; set; }

        [Params(false, true)]
        public bool UseLatestTable { get; set; }

        [GlobalSetup(Target = nameof(GetMergedAttributes))]
        public async Task Setup()
        {
            Setup(WithModelCaching, WithEffectiveTraitCaching);
            await SetupData();

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            using var mc = modelContextBuilder!.BuildImmediate();
            layerset = layerModel!.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task GetMergedAttributes()
        {
            BaseAttributeModel._USE_LATEST_TABLE = UseLatestTable;

            using var mc = modelContextBuilder!.BuildImmediate();

            var selection = CIIDSelection switch
            {
                "all" => new AllCIIDsSelection(),
                "specific" => specificCIIDs!,
                "allExcept" => allExceptCIIDs!,
                _ => throw new Exception() // must not happen
            };

            (await attributeModel!.GetMergedAttributes(selection, layerset!, mc, TimeThreshold.BuildLatest())).Consume(consumer);
        }

        [GlobalCleanup(Target = nameof(GetMergedAttributes))]
        public void TearDownT() => TearDown();

        private async Task SetupData()
        {
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());

            var random = new Random(3);

            ciids = Enumerable.Range(0, AttributeCITuple.numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            layerNames = Enumerable.Range(0, AttributeCITuple.numLayers).Select(i =>
            {
                var identity = "l_" + RandomUtility.GenerateRandomString(8, random, "abcdefghijklmnopqrstuvwxy");
                return identity;
            }).ToList();

            var changeset = new ChangesetProxy(user, TimeThreshold.BuildLatest(), changesetModel);

            using var mc = modelContextBuilder.BuildDeferred();
            var cis = ciids.Select(identity =>
            {
                return (ciModel.CreateCI(identity, mc).GetAwaiter().GetResult(), identity);
            }).ToList();

            specificCIIDs = SpecificCIIDsSelection.Build(ciids.Take(ciids.Count / 2).ToHashSet());
            allExceptCIIDs = AllCIIDsExceptSelection.Build(ciids.Take(ciids.Count / 2).ToHashSet());

            var layers = layerNames.Select(identity =>
            {
                return layerModel.UpsertLayer(identity, mc).GetAwaiter().GetResult();
            }).ToList();

            var attributeNames = Enumerable.Range(0, AttributeCITuple.numAttributeNames).Select(i => "A" + RandomUtility.GenerateRandomString(32, random)).ToList();
            var attributes = Enumerable.Range(0, AttributeCITuple.numAttributeInserts).Select(i =>
            {
                var name = attributeNames.GetRandom(random);
                var value = new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random));
                var layer = layers.GetRandom(random);
                var ciid = cis.GetRandom(random).Item1;
                return attributeModel.InsertAttribute(name!, value, ciid, layer!.ID, changeset, new DataOriginV1(DataOriginType.Manual), mc).GetAwaiter().GetResult();
            }).ToList();

            mc.Commit();
        }
    }
}
