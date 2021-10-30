using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Diagnostics;

namespace Omnikeeper.Runners
{
    public class UsageDataWriteRunner
    {
        private readonly IUsageDataAccumulator usageDataAccumulator;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<UsageDataWriteRunner> logger;

        public UsageDataWriteRunner(IUsageDataAccumulator usageDataAccumulator, IModelContextBuilder modelContextBuilder, ILogger<UsageDataWriteRunner> logger)
        {
            this.usageDataAccumulator = usageDataAccumulator;
            this.modelContextBuilder = modelContextBuilder;
            this.logger = logger;
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

                var deleteThreshold = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(30));

                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    usageDataAccumulator.Flush(trans);
                    var numDeleted = usageDataAccumulator.DeleteOlderThan(deleteThreshold, trans).GetAwaiter().GetResult();
                    if (numDeleted > 0)
                    {
                        logger.LogInformation($"Deleted {numDeleted} usage stats entries that were older than {deleteThreshold}");
                    }
                    trans.Commit();
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                logger.LogInformation($"Finished in {elapsedTime}");
            }
        }
    }
}
