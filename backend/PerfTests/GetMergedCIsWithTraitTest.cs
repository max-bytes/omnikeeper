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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tests;
using Tests.Integration;

namespace PerfTests
{
    public class GetMergedCIsWithTraitTest : Base
    {
        [GlobalSetup(Target = nameof(GetMergedCIsWithTraitWithoutPartitioning))]
        public async Task SetupWithoutPartitioning() => await SetupGeneric(false, true);
        [Benchmark]
        public async Task GetMergedCIsWithTraitWithoutPartitioning()
        {
            using var mc = ModelContextBuilder.BuildImmediate();
            (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time)).Consume(consumer);
        }

        [GlobalSetup(Target = nameof(GetMergedCIsWithTrait))]
        public async Task SetupWithPartitioning() => await SetupGeneric(true, true);
        [Benchmark]
        public async Task GetMergedCIsWithTrait()
        {
            using var mc = ModelContextBuilder.BuildImmediate();
            (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time)).Consume(consumer);
        }

        [GlobalSetup(Target = nameof(GetMergedCIsWithTraitWithoutCaching))]
        public async Task SetupWithPartitioningWithoutCaching() => await SetupGeneric(true, false);
        [Benchmark]
        public async Task GetMergedCIsWithTraitWithoutCaching()
        {
            using var mc = ModelContextBuilder.BuildImmediate();
            (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time)).Consume(consumer);
        }

        private IEffectiveTraitModel effectiveTraitModel;
        private Trait traitHost;
        private Trait traitLinuxHost;
        private LayerSet layerset;
        private TimeThreshold time;
        private readonly Consumer consumer = new Consumer();

        public async Task SetupGeneric(bool runPartitioning, bool enableCaching)
        {
            Setup(enableCaching);

            var numCIs = 5000;
            var numLayers = 4;
            var numAttributeInserts = 50000;
            var numDataTransactions = 1;

            var layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, false, ServiceProvider, ModelContextBuilder);

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var traitsProvider = ServiceProvider.GetRequiredService<ITraitsProvider>();
            effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();

            using var mc = ModelContextBuilder.BuildImmediate();

            layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
            time = TimeThreshold.BuildLatest();
            traitHost = await traitsProvider.GetActiveTrait("host", mc, time);
            traitLinuxHost = await traitsProvider.GetActiveTrait("host_linux", mc, time);

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
        public void Run()
        {
            var summary = BenchmarkRunner.Run<GetMergedCIsWithTraitTest>();
        }
    }
}
