﻿using BenchmarkDotNet.Attributes;
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
using System.Linq;
using System.Threading.Tasks;
using Tests;

namespace PerfTests
{
    //[SimpleJob(RunStrategy.Monitoring, launchCount: 0, warmupCount: 0, targetCount: 1)]
    public class GetMergedCIsWithTraitTest : Base
    {
        //[GlobalSetup(Target = nameof(GetMergedCIsWithTraitWithoutPartitioning))]
        //public async Task SetupWithoutPartitioning() => await SetupGeneric(false, true);
        //[Benchmark]
        //public async Task GetMergedCIsWithTraitWithoutPartitioning()
        //{
        //    using var mc = modelContextBuilder.BuildImmediate();
        //    (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, selectedCIIDs, mc, time)).Consume(consumer);
        //}

        [GlobalSetup(Target = nameof(GetMergedCIsWithTrait))]
        public async Task Setup() => await SetupGeneric(false, WithCaching);
        [Benchmark]
        public async Task GetMergedCIsWithTrait()
        {
            using var mc = modelContextBuilder.BuildImmediate();
            var ciSelection = (SpecificCIs) ? selectedCIIDs : new AllCIIDsSelection();
            (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, ciSelection, mc, time)).Consume(consumer);
        }
        [GlobalCleanup(Target = nameof(GetMergedCIsWithTrait))]
        public void TearDownT() => TearDown();


        //[GlobalSetup(Target = nameof(GetMergedCIsWithTraitWithoutCaching))]
        //public async Task SetupWithPartitioningWithoutCaching() => await SetupGeneric(true, false);
        //[Benchmark]
        //public async Task GetMergedCIsWithTraitWithoutCaching()
        //{
        //    using var mc = modelContextBuilder.BuildImmediate();
        //    (await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, selectedCIIDs, mc, time)).Consume(consumer);
        //}
        //[GlobalCleanup(Target = nameof(GetMergedCIsWithTraitWithoutCaching))]
        //public void TearDownWithoutCaching() => TearDown();

        private IEffectiveTraitModel effectiveTraitModel;
        private IModelContextBuilder modelContextBuilder;
        private Trait traitHost;
        private LayerSet layerset;
        private TimeThreshold time;
        private ICIIDSelection selectedCIIDs;
        private readonly Consumer consumer = new Consumer();

        [ParamsSource(nameof(ValuesForA))]
        public (int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions) AttributeCITuple { get; set; }
        public IEnumerable<(int numCIs, int numAttributeInserts, int numLayers, int numDataTransactions)> ValuesForA => new[] { (5000, 50000, 4, 1) };

        [Params(true, false)]
        public bool WithCaching { get; set; }

        [Params(true)]
        public bool SpecificCIs { get; set; }

        public async Task SetupGeneric(bool runPartitioning, bool enableCaching)
        {
            Setup(enableCaching);

            var numCIs = AttributeCITuple.numCIs;
            var numLayers = AttributeCITuple.numLayers;
            var numAttributeInserts = AttributeCITuple.numAttributeInserts;
            var numDataTransactions = AttributeCITuple.numDataTransactions;

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var traitsProvider = ServiceProvider.GetRequiredService<ITraitsProvider>();
            effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();

            var layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, false, ServiceProvider, modelContextBuilder);

            using var mc = modelContextBuilder.BuildImmediate();
            layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
            time = TimeThreshold.BuildLatest();
            traitHost = await traitsProvider.GetActiveTrait("host", mc, time);

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
        public void Run()
        {
            var summary = BenchmarkRunner.Run<GetMergedCIsWithTraitTest>();
        }
    }
}
