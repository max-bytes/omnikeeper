﻿using Hangfire;
using Hangfire.Server;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Runners
{
    public class ArchiveOldDataRunner
    {
        private readonly ILogger<ArchiveOldDataRunner> logger;
        private readonly IExternalIDMapPersister externalIDMapPersister;
        private readonly IChangesetModel changesetModel;
        private readonly ICIModel ciModel;
        private readonly NpgsqlConnection conn;

        public ArchiveOldDataRunner(ILogger<ArchiveOldDataRunner> logger, IExternalIDMapPersister externalIDMapPersister, IChangesetModel changesetModel, ICIModel ciModel, NpgsqlConnection conn)
        {
            this.logger = logger;
            this.externalIDMapPersister = externalIDMapPersister;
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

                var numArchivedChangesets = await changesetModel.ArchiveUnusedChangesetsOlderThan(threshold, trans);

                if (numArchivedChangesets > 0)
                    logger.LogInformation($"Archived {numArchivedChangesets} changesets because they are unused and older than {threshold}");

                trans.Commit();
            }

            // remove unused CIs
            // approach: unused CIs are CIs that are completely empty (no attributes for relations relate to it) AND
            // are not used in any OIA external ID mappings
            var numArchivedCIs = await ArchiveUnusedCIsService.ArchiveUnusedCIs(externalIDMapPersister, conn, logger);

            if (numArchivedCIs > 0)
                logger.LogInformation($"Archived {numArchivedCIs} CIs because they are unused");

        }

        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        [AutomaticRetry(Attempts = 0)]
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
