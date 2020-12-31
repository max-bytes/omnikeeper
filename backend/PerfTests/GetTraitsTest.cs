using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
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
    class GetTraitsTest : DIServicedTestBase
    {
        private IEnumerable<string> layerNames = new List<string>();

        public GetTraitsTest() : base(true)
        {
        }

        public async Task SetupData()
        {
            var timer = new Stopwatch();
            timer.Start();

            var numCIs = 50000;
            var numLayers = 4;
            var numAttributeInserts = 500000;
            var numDataTransactions = 1;

            layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, false, ServiceProvider, ModelContextBuilder);

            timer.Stop();
            Console.WriteLine($"Setup - Elapsed time: {timer.ElapsedMilliseconds / 1000f}");
        }

        [Test]
        public async Task TestGetMergedCIsWithTrait()
        {
            await SetupData();

            // NOTE: optimizing postgres with
            // SET random_page_cost = 1.1;
            // produces much better results as the indices are used more often by the query planer, at least in test scenarios
            // further research required, also see https://www.postgresql.org/docs/12/runtime-config-query.html#RUNTIME-CONFIG-QUERY-CONSTANTS

            var random = new Random(3);

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            var ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            var attributeModel = ServiceProvider.GetRequiredService<IAttributeModel>();
            var effectiveTraitModel = ServiceProvider.GetRequiredService<IEffectiveTraitModel>();
            var traitsProvider = ServiceProvider.GetRequiredService<ITraitsProvider>();

            using var mc = ModelContextBuilder.BuildImmediate();

            var layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
            var time = TimeThreshold.BuildLatest();
            var traitHost = await traitsProvider.GetActiveTrait("host", mc, time);
            var traitLinuxHost = await traitsProvider.GetActiveTrait("linux_host", mc, time);

            var cis = Time("First fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time);
            });
            Console.WriteLine($"Count: {cis.Count()}");
            Console.WriteLine("Attributes of random CI");
            foreach (var aa in cis.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }

            var cis2 = Time("Second fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time);
            });
            var cis3 = Time("Third fetch of CIs with trait host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitHost, layerset, new AllCIIDsSelection(), mc, time);
            });
            Console.WriteLine("Attributes of random CI 3");
            foreach (var aa in cis3.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }


            var cis4 = Time("First fetch of CIs with trait linux-host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, new AllCIIDsSelection(), mc, time);
            });
            Console.WriteLine($"Count: {cis4.Count()}");
            Console.WriteLine("Attributes of random CI");
            foreach (var aa in cis4.GetRandom(random).MergedAttributes)
            {
                Console.WriteLine($"{aa.Value.Attribute.State} {aa.Value.LayerStackIDs[^1]} {aa.Value.Attribute.Name}: {aa.Value.Attribute.Value.Value2String()} ");
            }

            var cis5 = Time("Second fetch of CIs with trait linux-host", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, new AllCIIDsSelection(), mc, time);
            });

            // fetch CIs directly, as comparison
            var ciids = cis5.Select(ci => ci.ID);
            var cis6 = Time("Fetching CIs directly", async () =>
            {
                return await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciids), layerset, false, mc, time);
            });

            // perform a partitioning
            var dataPartitionService = ServiceProvider.GetRequiredService<IDataPartitionService>();
            Assert.IsTrue(await dataPartitionService.StartNewPartition());

            var cis7 = Time("First fetch of CIs with trait linux-host after partitioning", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, new AllCIIDsSelection(), mc, time);
            });
            var cis8 = Time("Second fetch of CIs with trait linux-host after partitioning", async () =>
            {
                return await effectiveTraitModel.GetMergedCIsWithTrait(traitLinuxHost, layerset, new AllCIIDsSelection(), mc, time);
            });

            // fetch CIs directly, as comparison
            var cis9 = Time("Fetching CIs directly after partitioning", async () =>
            {
                return await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciids), layerset, false, mc, time);
            });
        }

        private R Time<R>(string name, Func<Task<R>> f)
        {
            Console.WriteLine($"---");
            var timer = new Stopwatch();
            timer.Start();
            var result = f().GetAwaiter().GetResult();
            timer.Stop();
            Console.WriteLine($"{name} - Elapsed Time: {timer.ElapsedMilliseconds / 1000f}");
            return result;
        }
    }
}
