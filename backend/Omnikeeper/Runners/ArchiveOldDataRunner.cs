using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class ArchiveOldDataRunner
    {
        private readonly ILogger<ArchiveOldDataRunner> logger;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly IChangesetModel changesetModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public ArchiveOldDataRunner(ILogger<ArchiveOldDataRunner> logger, IExternalIDMapPersister externalIDMapPersister, 
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel,
            IChangesetModel changesetModel, IMetaConfigurationModel metaConfigurationModel, IBaseConfigurationModel baseConfigurationModel, ILayerModel layerModel, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.externalIDMapPersister = externalIDMapPersister;
            this.baseAttributeRevisionistModel = baseAttributeRevisionistModel;
            this.baseRelationRevisionistModel = baseRelationRevisionistModel;
            this.changesetModel = changesetModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        public async Task RunAsync()
        {
            var now = TimeThreshold.BuildLatest();
            // delete outdated attributes and relations (empty changesets are deleted afterwards)
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
                var baseConfiguration = await baseConfigurationModel.GetConfigOrDefault(new Base.Entity.LayerSet(metaConfiguration.ConfigLayerset), now, trans);
                var archiveThreshold = baseConfiguration.ArchiveDataThreshold;

                if (archiveThreshold == BaseConfigurationV2.InfiniteArchiveDataThreshold)
                {
                    return;
                }
                var threshold = DateTimeOffset.Now.Add(archiveThreshold.Negate());

               logger.LogDebug($"Deleting outdated attributes and relations older than {threshold}");

                var layerIDs = (await layerModel.GetLayers(trans)).Select(l => l.ID).ToArray();
                var numDeletedAttributes = await baseAttributeRevisionistModel.DeleteOutdatedAttributesOlderThan(layerIDs, trans, threshold, now);
                var numDeletedRelations = await baseRelationRevisionistModel.DeleteOutdatedRelationsOlderThan(layerIDs, trans, threshold, now);
                if (numDeletedAttributes > 0)
                    logger.LogInformation($"Deleted {numDeletedAttributes} attributes because they were outdated and older than {threshold}");
                if (numDeletedRelations > 0)
                    logger.LogInformation($"Deleted {numDeletedRelations} relations because they were outdated and older than {threshold}");

                trans.Commit();
                logger.LogDebug($"Done deleting outdated attributes and relations");
            }

            // archive empty changesets
            // NOTE: several procedures exist that can delete attributes/relations, but do not check if the associated changeset becomes empty
            // that's why we need a procedure here that checks for empty changesets and deletes them
            logger.LogDebug($"Deleting empty changesets");
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var numDeletedEmptyChangesets = await changesetModel.DeleteEmptyChangesets(trans);
                if (numDeletedEmptyChangesets > 0)
                    logger.LogInformation($"Deleted {numDeletedEmptyChangesets} changesets because they were empty");

                trans.Commit();
            }
            logger.LogDebug($"Done deleting empty changesets");

            // remove unused CIs
            // approach: unused CIs are CIs that are completely empty (no attributes for relations relate to it) AND
            // are not used in any OIA external ID mappings
            logger.LogDebug($"Archiving unused CIs");
            var numArchivedCIs = await ArchiveUnusedCIsService.ArchiveUnusedCIs(externalIDMapPersister, modelContextBuilder, logger);
            if (numArchivedCIs > 0)
                logger.LogInformation($"Archived {numArchivedCIs} CIs because they are unused");
            logger.LogDebug($"Done archiving unused CIs");

        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(PerformContext? context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                logger.LogInformation("Start");

                RunAsync().GetAwaiter().GetResult();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                logger.LogInformation($"Finished in {elapsedTime}");
            }
        }



    }
}
