using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model;
using Omnikeeper.Model.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tests;

namespace PerfTests
{
    // TODO: since latest changes, this perf test does not make much sense anymore, consider rework or removal

    [Explicit]
    public class GetMergedCIsWithTraitTest : Base
    {
        [GlobalSetup(Target = nameof(GetMergedCIsWithTrait))]
        public async Task Setup() => await SetupGeneric(false, WithModelCaching, WithEffectiveTraitCaching);

        [Benchmark]
        public async Task GetMergedCIsWithTrait()
        {
            BaseAttributeModel._USE_LATEST_TABLE = UseLatestTable;
            using var mc = modelContextBuilder!.BuildImmediate();
            var ciSelection = (SpecificCIs) ? selectedCIIDs : new AllCIIDsSelection();
            var cis = await ciModel!.GetMergedCIs(ciSelection!, layerset!, false, AllAttributeSelection.Instance, mc, time);
            (await effectiveTraitModel!.FilterCIsWithTrait(cis, trait!, layerset!, mc, time)).Consume(consumer);

            // second time should hit cache
            (await effectiveTraitModel!.FilterCIsWithTrait(cis, trait!, layerset!, mc, time)).Consume(consumer);
        }

        [Test]
        public void RunBenchmark()
        {
            var summary = BenchmarkRunner.Run<GetMergedCIsWithTraitTest>();
        }

        [GlobalCleanup(Target = nameof(GetMergedCIsWithTrait))]
        public void TearDownT() => TearDown();

        private IEffectiveTraitModel? effectiveTraitModel;
        private ICIModel? ciModel;
        private IModelContextBuilder? modelContextBuilder;
        private ITrait? trait;
        private LayerSet? layerset;
        private TimeThreshold time;
        private ICIIDSelection? selectedCIIDs;
        private readonly Consumer consumer = new Consumer();

        [ParamsSource(nameof(AttributeCITuples))]
        public (int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions)> AttributeCITuples => new[] {
            //(50, 500, 4, 1),
            (5000, 50000, 4, 1),
            //(100000, 1000000, 4, 1),
        };

        [Params(false)]
        public bool WithModelCaching { get; set; }

        [Params(false, true)]
        public bool UseLatestTable { get; set; }

        [Params(false, true)]
        public bool WithEffectiveTraitCaching { get; set; }

        [Params("host", "host_linux")]
        public string? TraitToFetch { get; set; }

        [Params(false)]
        public bool SpecificCIs { get; set; }

        public async Task SetupGeneric(bool runPartitioning, bool enableModelCaching, bool enableEffectiveTraitCaching)
        {
            Setup(enableModelCaching, enableEffectiveTraitCaching, true);

            var numCIs = AttributeCITuple.numCIs;
            var numLayers = AttributeCITuple.numLayers;
            var numAttributeInserts = AttributeCITuple.numAttributeInserts;
            var numDataTransactions = AttributeCITuple.numDataTransactions;

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var traitsProvider = ServiceProvider.GetRequiredService<ITraitsProvider>();
            effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();

            using var mc = modelContextBuilder.BuildImmediate();

            var layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, true, ServiceProvider, modelContextBuilder);
            layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();

            time = TimeThreshold.BuildLatest();

            trait = await traitsProvider.GetActiveTrait(TraitToFetch!, mc, time);

            var allCIIDs = await ciModel.GetCIIDs(mc);
            var random = new Random(3);
            selectedCIIDs = SpecificCIIDsSelection.Build(allCIIDs.Where(ciid => random.Next(0, 2 + 1) == 0).ToHashSet());

            if (runPartitioning)
            {
                var dataPartitionService = ServiceProvider.GetRequiredService<IDataPartitionService>();
                await dataPartitionService.StartNewPartition();
            }

            // NOTE: optimizing postgres with
            // SET random_page_cost = 1.1;
            // produces much better results as the indices are used more often by the query planer, at least in test scenarios
            // further research required, also see https://www.postgresql.org/docs/12/runtime-config-query.html#RUNTIME-CONFIG-QUERY-CONSTANTS
        }

        [Test]
        public async Task RunDebuggable()
        {
            TraitToFetch = "host";
            AttributeCITuple = AttributeCITuples.First();
            await SetupGeneric(false, false, true);
            await GetMergedCIsWithTrait();
            TearDown();
        }
    }
}
