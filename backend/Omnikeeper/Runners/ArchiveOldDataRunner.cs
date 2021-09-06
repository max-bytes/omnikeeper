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
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class ArchiveOldDataRunner
    {
        private readonly ILogger<ArchiveOldDataRunner> logger;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly IPartitionModel partitionModel;
        private readonly IChangesetModel changesetModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public ArchiveOldDataRunner(ILogger<ArchiveOldDataRunner> logger, IExternalIDMapPersister externalIDMapPersister, 
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel, IPartitionModel partitionModel,
            IChangesetModel changesetModel, IBaseConfigurationModel baseConfigurationModel, ILayerModel layerModel, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.externalIDMapPersister = externalIDMapPersister;
            this.baseAttributeRevisionistModel = baseAttributeRevisionistModel;
            this.baseRelationRevisionistModel = baseRelationRevisionistModel;
            this.partitionModel = partitionModel;
            this.changesetModel = changesetModel;
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
                var cfg = await baseConfigurationModel.GetConfigOrDefault(trans);
                var archiveThreshold = cfg.ArchiveChangesetThreshold; // TODO: rename to something better fitting

                if (archiveThreshold == BaseConfigurationV1.InfiniteArchiveChangesetThreshold)
                {
                    return;
                }
                var threshold = DateTimeOffset.Now.Add(archiveThreshold.Negate());

                logger.LogDebug($"Deleting outdated attributes and relations older than {threshold}");
    
                var numDeletedAttributes = 0;
                var numDeletedRelations = 0;
                foreach (var layer in await layerModel.GetLayers(trans))
                {
                    numDeletedAttributes += await baseAttributeRevisionistModel.DeleteOutdatedAttributesOlderThan(layer.ID, trans, threshold, now);
                    numDeletedRelations += await baseRelationRevisionistModel.DeleteOutdatedRelationsOlderThan(layer.ID, trans, threshold, now);
                }
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


            //logger.LogDebug($"Rebuilding latest tables");
            //using (var trans = modelContextBuilder.BuildDeferred())
            //{
            //    await RebuildLatestTablesService.RebuildLatestAttributesTable(partitionModel, layerModel, trans);
            //    trans.Commit();
            //}
            //logger.LogDebug($"Done rebuilding latest tables");
        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(PerformContext? context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                logger.LogInformation("Start");
                RunAsync().GetAwaiter().GetResult();
                logger.LogInformation("Finished");
            }
        }



    }
}
