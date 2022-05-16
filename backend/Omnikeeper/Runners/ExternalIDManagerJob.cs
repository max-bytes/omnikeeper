using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class ExternalIDManagerJob : IJob
    {
        private readonly ILogger<ExternalIDManagerJob> logger;
        private readonly IInboundAdapterManager pluginManager;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly ICIModel ciModel;
        private readonly CIMappingService ciMappingService;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly ILayerDataModel layerDataModel;
        private readonly IModelContextBuilder modelContextBuilder;

        // HACK: making this static sucks, find better way, but runner is instantiated anew on each run
        // TODO: not supported in HA/cluster scenario
        private static readonly IDictionary<string, DateTimeOffset> lastRuns = new ConcurrentDictionary<string, DateTimeOffset>();

        public ExternalIDManagerJob(IInboundAdapterManager pluginManager, IExternalIDMapPersister externalIDMapPersister, ICIModel ciModel, CIMappingService ciMappingService,
            IAttributeModel attributeModel, IRelationModel relationModel, ILayerDataModel layerDataModel, IModelContextBuilder modelContextBuilder, ILogger<ExternalIDManagerJob> logger)
        {
            this.logger = logger;
            this.pluginManager = pluginManager;
            this.externalIDMapPersister = externalIDMapPersister;
            this.ciModel = ciModel;
            this.ciMappingService = ciMappingService;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.layerDataModel = layerDataModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var trans = modelContextBuilder.BuildImmediate();
                var activeLayers = await layerDataModel.GetLayerData(Base.Entity.AnchorStateFilter.ActiveAndDeprecated, trans, TimeThreshold.BuildLatest());
                var layersWithOILPs = activeLayers.Where(l => l.OIAReference != "");

                var adapters = layersWithOILPs.Select(l => l.OIAReference)
                    .Distinct(); // distinct because multiple layers can have the same adapter configured

                var usedPersisterScopes = new HashSet<string>();

                foreach (var adapterName in adapters)
                {

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

                        var foundLastRun = lastRuns.TryGetValue(adapterName, out var lastRun);
                        if (!foundLastRun || (DateTimeOffset.Now - lastRun) > EIDManager.PreferredUpdateRate)
                        {
                            logger.LogInformation($"Running external ID update for OILP {adapterName}");

                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            try
                            {
                                using var transD = modelContextBuilder.BuildDeferred();

                                var (changes, successful) = await EIDManager.Update(ciModel, attributeModel, relationModel, ciMappingService, transD, logger);

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

                logger.LogTrace("Finished");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running external-id-manager job");
            }
        }
    }
}
