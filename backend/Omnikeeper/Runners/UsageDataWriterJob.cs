using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Quartz;
using System;
using System.Diagnostics;
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

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
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

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                logger.LogTrace($"Finished in {elapsedTime}");
            }
            catch (Exception e)
            {
                logger.LogError("Error running usage-data-writer job", e);
            }
        }
    }
}
