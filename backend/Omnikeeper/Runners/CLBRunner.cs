using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains,
            ILayerModel layerModel, ILogger<CLBRunner> logger, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.layerModel = layerModel;
            this.logger = logger;
            this.modelContextBuilder = modelContextBuilder;
        }

        // TODO: enable and test disabling of concurrent execution
        //[DisableConcurrentExecution(timeoutInSeconds: 60)]
        //[AutomaticRetry(Attempts = 0)]
        public void Run(PerformContext? context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                RunAsync().GetAwaiter().GetResult();
            }
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            var trans = modelContextBuilder.BuildImmediate();
            var activeLayers = await layerModel.GetLayers(Omnikeeper.Base.Entity.AnchorStateFilter.ActiveAndDeprecated, trans);
            var layersWithCLBs = activeLayers.Where(l => l.ComputeLayerBrainLink.Name != ""); // TODO: better check for set clb than name != ""

            foreach (var l in layersWithCLBs)
            {
                // find clb for layer
                if (!existingComputeLayerBrains.TryGetValue(l.ComputeLayerBrainLink.Name, out var clb))
                {
                    logger.LogError($"Could not find compute layer brain with name {l.ComputeLayerBrainLink.Name}");
                }
                else
                {
                    logger.LogInformation($"Running CLB {l.ComputeLayerBrainLink.Name} on layer {l.Name}");

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    await clb.Run(new CLBSettings(l.Name), modelContextBuilder, logger);
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    logger.LogInformation($"Done in {elapsedTime}");
                }
            }

            logger.LogInformation("Finished");
        }

        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;
        private readonly ILayerModel layerModel;
        private readonly ILogger<CLBRunner> logger;
        private readonly IModelContextBuilder modelContextBuilder;
    }
}
