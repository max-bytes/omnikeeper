using GraphQL;
using Hangfire.Server;
using Landscape.Base.CLB;
using Landscape.Base.Model;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Runners
{
    public class CLBRunner
    {
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ILayerModel layerModel, ILogger<CLBRunner> logger)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.layerModel = layerModel;
            this.logger = logger;
        }

        // TODO: enable and test disabling of concurrent execution
        //[DisableConcurrentExecution(timeoutInSeconds: 60)]
        //[AutomaticRetry(Attempts = 0)]
        public void Run(PerformContext context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                RunAsync().GetAwaiter().GetResult();
            }
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            var activeLayers = await layerModel.GetLayers(Landscape.Base.Entity.AnchorStateFilter.ActiveAndDeprecated, null);
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
                    await clb.Run(new CLBSettings(l.Name), logger);
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
    }
}
