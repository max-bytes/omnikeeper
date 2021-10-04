using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
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
        public CLBRunner(IEnumerable<IComputeLayerBrain> existingComputeLayerBrains, ICLConfigModel clConfigModel,
            IBaseConfigurationModel baseConfigurationModel,
            ILayerModel layerModel, ILogger<CLBRunner> logger, IModelContextBuilder modelContextBuilder)
        {
            this.existingComputeLayerBrains = existingComputeLayerBrains.ToDictionary(l => l.Name);
            this.clConfigModel = clConfigModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.layerModel = layerModel;
            this.logger = logger;
            this.modelContextBuilder = modelContextBuilder;
        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
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

            var timeThreshold = TimeThreshold.BuildLatest();
            var trans = modelContextBuilder.BuildImmediate();
            var activeLayers = await layerModel.GetLayers(AnchorStateFilter.ActiveAndDeprecated, trans);
            var layersWithCLBs = activeLayers.Where(l => l.CLConfig != "");

            if (!layersWithCLBs.IsEmpty()) {
                var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(trans);
                var clConfigs = await clConfigModel.GetCLConfigs(new LayerSet(baseConfiguration.ConfigLayerset), trans, timeThreshold);

                foreach (var l in layersWithCLBs)
                {
                    // find clConfig for layer
                    if (!clConfigs.TryGetValue(l.CLConfig, out var clConfig)) 
                    {
                        logger.LogError($"Could not find cl config with ID {l.CLConfig}");
                    } else {
                        if (!existingComputeLayerBrains.TryGetValue(clConfig.CLBrainReference, out var clb))
                        {
                            logger.LogError($"Could not find compute layer brain with name {clConfig.CLBrainReference}");
                        }
                        else
                        {
                            logger.LogInformation($"Running CLB {clb.Name} on layer {l.ID}");

                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            await clb.Run(l, clConfig.CLBrainConfig, modelContextBuilder, logger);
                            stopWatch.Stop();
                            TimeSpan ts = stopWatch.Elapsed;
                            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                            logger.LogInformation($"Done in {elapsedTime}");
                        }
                    }
                }
            }

            logger.LogInformation("Finished");
        }

        private readonly IDictionary<string, IComputeLayerBrain> existingComputeLayerBrains;
        private readonly ICLConfigModel clConfigModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly ILayerModel layerModel;
        private readonly ILogger<CLBRunner> logger;
        private readonly IModelContextBuilder modelContextBuilder;
    }
}
