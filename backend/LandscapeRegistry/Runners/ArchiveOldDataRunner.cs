using Hangfire.Server;
using Landscape.Base.Model;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace LandscapeRegistry.Runners
{
    public class ArchiveOldDataRunner
    {
        private readonly ILogger<ArchiveOldDataRunner> logger;
        private readonly IChangesetModel changesetModel;
        private readonly ICIModel ciModel;
        private readonly NpgsqlConnection conn;

        public ArchiveOldDataRunner(ILogger<ArchiveOldDataRunner> logger, IChangesetModel changesetModel, ICIModel ciModel, NpgsqlConnection conn)
        {
            this.logger = logger;
            this.changesetModel = changesetModel;
            this.ciModel = ciModel;
            this.conn = conn;
        }

        public async Task RunAsync()
        {
            // remove outdated changesets
            using (var trans = conn.BeginTransaction()) {

                // TODO: make configurable
                var threshold = DateTimeOffset.Now.AddMonths(-3);

                var numArchived = await changesetModel.ArchiveUnusedChangesetsOlderThan(threshold, trans);

                if (numArchived > 0)
                    logger.LogInformation($"Archived {numArchived} changesets because they are unused and older than {threshold}");

                trans.Commit();
            }

            // remove unused CIs
            // approach: unused CIs are CIs that are completely empty (no attributes for relations relate to it) AND
            // are not used in any OIA external ID mappings
            using (var trans = conn.BeginTransaction())
            {
                var numArchived = await ciModel.ArchiveUnusedCIs(trans);

                if (numArchived > 0)
                    logger.LogInformation($"Archived {numArchived} CIs because they are unused");

                trans.Commit();
            }
        }

        public void Run(PerformContext context)
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
