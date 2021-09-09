using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
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
        private IAttributeModel? attributeModel;
        private LayerSet? layerset;
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

        [Params(true)]
        public bool PreSetupData { get; set; }

        [GlobalSetup(Target = nameof(GetMergedAttributes))]
        public async Task Setup()
        {
            Setup(WithModelCaching, WithEffectiveTraitCaching, PreSetupData);

            if (PreSetupData)
            {
                await SetupData();
            }

            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            using var mc = modelContextBuilder!.BuildImmediate();
            var layers = await layerModel.GetLayers(mc);
            layerset = layerModel!.BuildLayerSet(layers.Select(l => l.ID).ToArray(), mc).GetAwaiter().GetResult();

            var ciidModel = ServiceProvider.GetRequiredService<ICIIDModel>();
            var ciids = (await ciidModel.GetCIIDs(mc)).ToList();
            specificCIIDs = SpecificCIIDsSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
            allExceptCIIDs = AllCIIDsExceptSelection.Build(ciids.Take(ciids.Count / 3).ToHashSet());
        }

        [Benchmark]
        public async Task GetMergedAttributes()
        {
            BaseAttributeModel._USE_LATEST_TABLE = UseLatestTable;

            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            using var mc = modelContextBuilder.BuildImmediate();

            var selection = CIIDSelection switch
            {
                "all" => new AllCIIDsSelection(),
                "specific" => specificCIIDs!,
                "allExcept" => allExceptCIIDs!,
                _ => throw new Exception() // must not happen
            };

            (await attributeModel!.GetMergedAttributes(selection, layerset!, mc, TimeThreshold.BuildLatest())).Consume(consumer);
        }

        [Test]
        public void RunBenchmark()
        {
            var summary = BenchmarkRunner.Run<GetMergedAttributesTest>();
        }


        [Test]
        public async Task RunDebuggable()
        {
            CIIDSelection = "specific";
            WithModelCaching = false;
            WithEffectiveTraitCaching = false;
            UseLatestTable = true;
            AttributeCITuple = AttributeCITuples.First();
            await Setup();
            await GetMergedAttributes();
            TearDown();
        }

        [GlobalCleanup(Target = nameof(GetMergedAttributes))]
        public void TearDownT() => TearDown();

        private async Task SetupData()
        {
            var changesetModel = ServiceProvider.GetRequiredService<IChangesetModel>();
            var modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var userModel = ServiceProvider.GetRequiredService<IUserInDatabaseModel>();
            var user = await DBSetup.SetupUser(userModel, modelContextBuilder.BuildImmediate());

            var random = new Random(3);

            var ciids = Enumerable.Range(0, AttributeCITuple.numCIs).Select(i =>
            {
                return Guid.NewGuid();
            }).ToList();

            var layerNames = Enumerable.Range(0, AttributeCITuple.numLayers).Select(i =>
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

            var layers = layerNames.Select(identity =>
            {
                return layerModel.UpsertLayer(identity, mc).GetAwaiter().GetResult();
            }).ToList();

            var attributeNames = Enumerable.Range(0, AttributeCITuple.numAttributeNames).Select(i => "A" + RandomUtility.GenerateRandomString(32, random)).ToList();
            foreach (var layer in layers) {
                var usedAttributes = new HashSet<string>();
                var fragments = Enumerable.Range(0, AttributeCITuple.numAttributeInserts).Select(i =>
                {
                    var found = false;
                    string name = "";
                    Guid ciid = new Guid();
                    while(!found)
                    {
                        name = attributeNames.GetRandom(random)!;
                        ciid = cis.GetRandom(random).Item1;
                        var hash = CIAttribute.CreateInformationHash(name, ciid);
                        if (!usedAttributes.Contains(hash))
                        {
                            usedAttributes.Add(hash);
                            found = true;
                        }
                    }
                    var value = new AttributeScalarValueText("V" + RandomUtility.GenerateRandomString(8, random));
                    return new BulkCIAttributeDataLayerScope.Fragment(name, value, ciid);
                });

                var data = new BulkCIAttributeDataLayerScope("", layer!.ID, fragments);

                await attributeModel.BulkReplaceAttributes(data, changeset, new DataOriginV1(DataOriginType.Manual), mc);
            }

            mc.Commit();
        }
    }
}
