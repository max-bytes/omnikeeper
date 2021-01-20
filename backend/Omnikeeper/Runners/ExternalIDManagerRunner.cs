using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class ExternalIDManagerRunner
    {
        private readonly ILogger<ExternalIDManagerRunner> logger;
        private readonly IInboundAdapterManager pluginManager;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly ICIModel ciModel;
        private readonly CIMappingService ciMappingService;
        private readonly IAttributeModel attributeModel;
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;

        // HACK: making this static sucks, find better way, but runner is instantiated anew on each run
        private static readonly IDictionary<string, DateTimeOffset> lastRuns = new ConcurrentDictionary<string, DateTimeOffset>();

        public ExternalIDManagerRunner(IInboundAdapterManager pluginManager, IExternalIDMapPersister externalIDMapPersister, ICIModel ciModel, CIMappingService ciMappingService,
            IAttributeModel attributeModel, ILayerModel layerModel, IModelContextBuilder modelContextBuilder, ILogger<ExternalIDManagerRunner> logger)
        {
            this.logger = logger;
            this.pluginManager = pluginManager;
            this.externalIDMapPersister = externalIDMapPersister;
            this.ciModel = ciModel;
            this.ciMappingService = ciMappingService;
            this.attributeModel = attributeModel;
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        [AutomaticRetry(Attempts = 0)]
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
            var layersWithOILPs = activeLayers.Where(l => l.OnlineInboundAdapterLink.AdapterName != ""); // TODO: better check for set oilp than name != ""

            var adapters = layersWithOILPs.Select(l => l.OnlineInboundAdapterLink.AdapterName)
                .Distinct(); // distinct because multiple layers can have the same adapter configured

            var usedPersisterScopes = new HashSet<string>();

            foreach (var adapterName in adapters)
            {
                lastRuns.TryGetValue(adapterName, out var lastRun);

                // find oilp for layer
                var plugin = await pluginManager.GetOnlinePluginInstance(adapterName, trans);
                if (plugin == null)
                {
                    logger.LogError($"Could not find online inbound layer plugin with name {adapterName}");
                }
                else
                {
                    var EIDManager = plugin.GetExternalIDManager();

                    usedPersisterScopes.Add(EIDManager.PersisterScope);

                    if (lastRun == null || (DateTimeOffset.Now - lastRun) > EIDManager.PreferredUpdateRate)
                    {
                        logger.LogInformation($"Running external ID update for OILP {adapterName}");

                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();
                        try
                        {
                            using var transD = modelContextBuilder.BuildDeferred();

                            var (changes, successful) = await EIDManager.Update(ciModel, attributeModel, ciMappingService, transD, logger);

                            if (!successful)
                            {
                                transD.Rollback();
                                throw new Exception("Error updating External ID manager");
                            }

                            if (changes)
                            {
                                transD.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, $"An error occured when updating external IDs for OILP {adapterName}");
                        }
                        stopWatch.Stop();
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                        logger.LogInformation($"Done in {elapsedTime}");
                        lastRuns[adapterName] = DateTimeOffset.Now;
                    }
                    else
                    {
                        logger.LogInformation($"Skipping external ID update for OILP {adapterName}");
                    }
                }
            }

            try
            {
                using var transD = modelContextBuilder.BuildDeferred();
                await externalIDMapPersister.DeleteUnusedScopes(usedPersisterScopes, transD);
                transD.Commit();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"An error occured when deleting unused persisted scopes");
            }

            logger.LogInformation("Finished");
        }
    }
}
