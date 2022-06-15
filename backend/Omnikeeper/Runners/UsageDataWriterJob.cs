using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class UsageDataWriterJob : IJob
    {
        private readonly IUsageDataAccumulator usageDataAccumulator;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<UsageDataWriterJob> logger;

        public UsageDataWriterJob(IUsageDataAccumulator usageDataAccumulator, IModelContextBuilder modelContextBuilder, ILogger<UsageDataWriterJob> logger)
        {
            this.usageDataAccumulator = usageDataAccumulator;
            this.modelContextBuilder = modelContextBuilder;
            this.logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start");

                var deleteThreshold = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(30));

                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    usageDataAccumulator.Flush(trans);
                    var numDeleted = usageDataAccumulator.DeleteOlderThan(deleteThreshold, trans).GetAwaiter().GetResult();
                    if (numDeleted > 0)
                    {
                        logger.LogTrace($"Deleted {numDeleted} usage stats entries that were older than {deleteThreshold}");
                    }
                    trans.Commit();
                }

                t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running usage-data-writer job");
            }

            return Task.CompletedTask;
        }
    }
}
