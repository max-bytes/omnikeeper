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
using System.Linq;
using System.Threading.Tasks;
using Tests;

namespace PerfTests
{
    //[SimpleJob(RunStrategy.Monitoring, launchCount: 0, warmupCount: 0, targetCount: 1)]
    public class GetCompactCIsTest : Base
    {

        [GlobalSetup(Target = nameof(GetCompactCIs))]
        public async Task Setup() => await SetupGeneric(true, true);
        [Benchmark]
        public async Task GetCompactCIs()
        {
            using var mc = modelContextBuilder!.BuildImmediate();
            (await ciModel!.GetCompactCIs(new AllCIIDsSelection(), layerset!, mc, time)).Consume(consumer);
        }
        [GlobalCleanup(Target = nameof(GetCompactCIs))]
        public void TearDown1() => TearDown();


        [GlobalSetup(Target = nameof(GetCompactCIsWithoutCaching))]
        public async Task SetupWithoutCaching() => await SetupGeneric(true, false);
        [Benchmark]
        public async Task GetCompactCIsWithoutCaching()
        {
            using var mc = modelContextBuilder!.BuildImmediate();
            (await ciModel!.GetCompactCIs(new AllCIIDsSelection(), layerset!, mc, time)).Consume(consumer);
        }
        [GlobalCleanup(Target = nameof(GetCompactCIsWithoutCaching))]
        public void TearDownWithoutCaching() => TearDown();

        private ICIModel? ciModel;
        private IModelContextBuilder? modelContextBuilder;
        private LayerSet? layerset;
        private TimeThreshold time;
        private readonly Consumer consumer = new Consumer();

        public async Task SetupGeneric(bool runPartitioning, bool enableCaching)
        {
            Setup(enableCaching);

            var numCIs = 500;
            var numLayers = 4;
            var numAttributeInserts = 5000;
            var numDataTransactions = 1;

            var layerModel = ServiceProvider.GetRequiredService<ILayerModel>();
            ciModel = ServiceProvider.GetRequiredService<ICIModel>();
            modelContextBuilder = ServiceProvider.GetRequiredService<IModelContextBuilder>();

            var layerNames = await ExampleDataSetup.SetupCMDBExampleData(numCIs, numLayers, numAttributeInserts, numDataTransactions, false, ServiceProvider, modelContextBuilder);

            //private ModelContextBuilder? modelContextBuilder;
            //protected ModelContextBuilder ModelContextBuilder => modelContextBuilder!;
            //modelContextBuilder = new ModelContextBuilder(, conn, NullLogger<IModelContext>.Instance, new ProtoBufDataSerializer());

            using var mc = modelContextBuilder.BuildImmediate();

            layerset = layerModel.BuildLayerSet(layerNames.ToArray(), mc).GetAwaiter().GetResult();
            time = TimeThreshold.BuildLatest();

            if (runPartitioning)
            {
                var dataPartitionService = ServiceProvider.GetRequiredService<IDataPartitionService>();
                await dataPartitionService.StartNewPartition();
            }
        }

        //[Test]
        //public void Run()
        //{
        //    var summary = BenchmarkRunner.Run<GetCompactCIsTest>();
        //}
    }
}
