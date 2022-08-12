using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class ArchiveOldDataJob : IJob
    {
        private readonly ILogger<ArchiveOldDataJob> logger;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly IArchiveOutdatedIssuesService archiveOutdatedIssuesService;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;
        private readonly IBaseRelationRevisionistModel baseRelationRevisionistModel;
        private readonly IArchiveOutdatedChangesetDataService archiveOutdatedChangesetDataService;
        private readonly IChangesetModel changesetModel;
        private readonly IMetaConfigurationModel metaConfigurationModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly ILayerModel layerModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public ArchiveOldDataJob(ILogger<ArchiveOldDataJob> logger, IExternalIDMapPersister externalIDMapPersister, IArchiveOutdatedIssuesService archiveOutdatedIssuesService,
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel, IBaseRelationRevisionistModel baseRelationRevisionistModel, IArchiveOutdatedChangesetDataService archiveOutdatedChangesetDataService,
            IChangesetModel changesetModel, IMetaConfigurationModel metaConfigurationModel, IBaseConfigurationModel baseConfigurationModel, ILayerModel layerModel, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.externalIDMapPersister = externalIDMapPersister;
            this.archiveOutdatedIssuesService = archiveOutdatedIssuesService;
            this.baseAttributeRevisionistModel = baseAttributeRevisionistModel;
            this.baseRelationRevisionistModel = baseRelationRevisionistModel;
            this.archiveOutdatedChangesetDataService = archiveOutdatedChangesetDataService;
            this.changesetModel = changesetModel;
            this.metaConfigurationModel = metaConfigurationModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.layerModel = layerModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                logger.LogInformation("Start");
                var t = new StopTimer();

                // remove outdated issues
                logger.LogDebug($"Archiving outdated issues");
                var numArchivedissues = await archiveOutdatedIssuesService.ArchiveOutdatedIssues(modelContextBuilder, logger);
                if (numArchivedissues > 0)
                    logger.LogInformation($"Archived {numArchivedissues} issues because they are outdated");
                logger.LogDebug($"Done archiving outdated issues");

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

                // archive changeset-data CIs that serve no purpose
                logger.LogDebug($"Deleting outdated changeset-data CIs");
                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    await archiveOutdatedChangesetDataService.Archive(logger, trans);
                    trans.Commit();
                }
                logger.LogDebug($"Done deleting outdated changeset-data CIs");

                // archive empty changesets
                // NOTE: several procedures exist that can delete attributes/relations, but do not check if the associated changeset becomes empty
                // that's why we need a procedure here that checks for empty changesets and deletes them
                // NOTE: we delete in batches (of 1), to get out of high load scenarios and still make progress
                logger.LogDebug($"Deleting empty changesets");
                int numDeletedEmptyChangesets = 0;
                int numDeletedEmptyChangesetsInRun = 0;
                do
                {
                    using (var trans = modelContextBuilder.BuildDeferred())
                    {
                        numDeletedEmptyChangesetsInRun = await changesetModel.DeleteEmptyChangesets(1, trans);
                        trans.Commit();
                    }
                    numDeletedEmptyChangesets += numDeletedEmptyChangesetsInRun;
                } while (numDeletedEmptyChangesetsInRun > 0);
                if (numDeletedEmptyChangesets > 0)
                    logger.LogInformation($"Deleted {numDeletedEmptyChangesets} changesets because they were empty");
                logger.LogDebug($"Done deleting empty changesets");

                // remove unused CIs
                // approach: unused CIs are CIs that are completely empty (no attributes for relations relate to it) AND
                // are not used in any OIA external ID mappings
                logger.LogDebug($"Archiving unused CIs");
                var numArchivedCIs = await ArchiveUnusedCIsService.ArchiveUnusedCIs(externalIDMapPersister, modelContextBuilder, logger);
                if (numArchivedCIs > 0)
                    logger.LogInformation($"Archived {numArchivedCIs} CIs because they are unused");
                logger.LogDebug($"Done archiving unused CIs");

                t.Stop((ts, elapsedTime) => logger.LogInformation($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running archive-old-data job");
            }
        }
    }
}
