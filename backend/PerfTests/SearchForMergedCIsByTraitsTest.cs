using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests;

namespace PerfTests
{
    [Explicit]
    public class SearchForMergedCIsByTraitsTest : Base
    {
        [GlobalSetup(Target = nameof(SearchForMergedCIsByTraits))]
        public async Task Setup() => await SetupGeneric(WithModelCaching, WithEffectiveTraitCaching);

        [Benchmark]
        public async Task SearchForMergedCIsByTraits()
        {
            using var mc = modelContextBuilder!.BuildImmediate();
            var ciSelection = (SpecificCIs) ? selectedCIIDs : new AllCIIDsSelection();
            (await ciSearchModel!.SearchForMergedCIsByTraits(ciSelection!, AllAttributeSelection.Instance, RequiredTraits!, new string[0], layerset!, mc, time)).Consume(consumer);

            // should hit cache, second time
            (await ciSearchModel!.SearchForMergedCIsByTraits(ciSelection!, AllAttributeSelection.Instance, RequiredTraits!, new string[0], layerset!, mc, time)).Consume(consumer);
        }

        [GlobalCleanup(Target = nameof(SearchForMergedCIsByTraits))]
        public void TearDownT() => TearDown();

        private ICISearchModel? ciSearchModel;
        private IModelContextBuilder? modelContextBuilder;
        private LayerSet? layerset;
        private TimeThreshold time;
        private ICIIDSelection? selectedCIIDs;
        private readonly Consumer consumer = new Consumer();

        [ParamsSource(nameof(AttributeCITuples))]
        public (int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions)> AttributeCITuples => new[] {
            (500, 5000, 4, 1),
            //(5000, 50000, 4, 1),
        };

        [Params(false)]
        public bool WithModelCaching { get; set; }

        [Params(false, true)]
        public bool WithEffectiveTraitCaching { get; set; }

        [ParamsSource(nameof(RequiredTraitsList))]
        public string[]? RequiredTraits { get; set; }
        public IEnumerable<string[]> RequiredTraitsList => new string[][] { new string[] { "host_linux" } };

        [Params(false)]
        public bool SpecificCIs { get; set; }

        public async Task SetupGeneric(bool enableModelCaching, bool enableEffectiveTraitCaching)
        {
            Setup(enableModelCaching, enableEffectiveTraitCaching, true);

            var numCIs = AttributeCITuple.numCIs;
            var numLayers = AttributeCITuple.numLayers;
            var numAttributeInserts = AttributeCITuple.numAttributeInserts;
            var numDataTransactions = AttributeCITuple.numDataTransactions;

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();
            ciSearchModel = ServiceProvider.GetRequiredService<ICISearchModel>();

            using var mc = modelContextBuilder.BuildImmediate();

            var layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, true, ServiceProvider, modelContextBuilder);
            layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();

            time = TimeThreshold.BuildLatest();

            var allCIIDs = await ciModel.GetCIIDs(mc);
            var random = new Random(3);
            selectedCIIDs = SpecificCIIDsSelection.Build(allCIIDs.Where(ciid => random.Next(0, 2 + 1) == 0).ToHashSet());
        }

        [Test]
        public void Run()
        {
            var summary = BenchmarkRunner.Run<SearchForMergedCIsByTraitsTest>();
        }
    }
}
