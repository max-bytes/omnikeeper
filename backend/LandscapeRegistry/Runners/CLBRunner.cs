using GraphQL;
using Hangfire;
using Landscape.Base;
using Landscape.Base.Model;
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

        //[DisableConcurrentExecution(timeoutInSeconds: 2 * 60)]
        public void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            var activeLayers = await layerModel.GetLayers(Landscape.Base.Entity.AnchorStateFilter.ActiveAndDeprecated, null);
            var layersWithCLBs = activeLayers.Where(l => l.ComputeLayerBrain.Name != ""); // TODO: better check for set clb than name != ""

            foreach (var l in layersWithCLBs)
            {
                // find clb for layer
                if (!existingComputeLayerBrains.TryGetValue(l.ComputeLayerBrain.Name, out var clb))
                {
                    logger.LogError($"Could not find compute layer brain with name {l.ComputeLayerBrain.Name}");
                }
                else
                {
                    logger.LogInformation($"Running CLB {l.ComputeLayerBrain.Name} on layer {l.Name}");

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
        private readonly IPredicateModel predicateModel;
        private readonly ILogger<CLBRunner> logger;
    }
}
