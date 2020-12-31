using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    public class ArchiveOldDataRunner
    {
        private readonly ILogger<ArchiveOldDataRunner> logger;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly IChangesetModel changesetModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public ArchiveOldDataRunner(ILogger<ArchiveOldDataRunner> logger, IExternalIDMapPersister externalIDMapPersister, 
            IChangesetModel changesetModel, IBaseConfigurationModel baseConfigurationModel, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.externalIDMapPersister = externalIDMapPersister;
            this.changesetModel = changesetModel;
            this.baseConfigurationModel = baseConfigurationModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        public async Task RunAsync()
        {
            // remove outdated changesets
            // this in turn also removes outdated attributes and relations
            logger.LogDebug($"Archiving outdated changesets");
            using (var trans = modelContextBuilder.BuildDeferred())
            {
                var cfg = await baseConfigurationModel.GetConfigOrDefault(trans);

                var archiveThreshold = cfg.ArchiveChangesetThreshold;

                if (archiveThreshold == BaseConfigurationV1.InfiniteArchiveChangesetThreshold)
                {
                    return;
                }

                //var threshold = DateTimeOffset.Now.Add(archiveThreshold.Negate());

                // TODO: rewrite to delete single attributes/relations (empty changeset deletion is handled later)
                // OR: think about data archiving rather in terms of partitioning!
                //var numArchivedChangesets = await changesetModel.ArchiveUnusedChangesetsOlderThan(threshold, trans);
                //if (numArchivedChangesets > 0)
                //    logger.LogInformation($"Archived {numArchivedChangesets} changesets because they are unused and older than {threshold}");

                trans.Commit();
            }
            logger.LogDebug($"Done archiving outdated changesets");

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

        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        [AutomaticRetry(Attempts = 0)]
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
