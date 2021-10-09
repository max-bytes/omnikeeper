using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Diagnostics;

namespace Omnikeeper.Runners
{
    public class MarkedForDeletionRunner
    {
        private readonly MarkedForDeletionService service;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ILogger<MarkedForDeletionRunner> logger;

        public MarkedForDeletionRunner(MarkedForDeletionService service, IModelContextBuilder modelContextBuilder, ILogger<MarkedForDeletionRunner> logger)
        {
            this.service = service;
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

                service.Run(modelContextBuilder, logger).GetAwaiter().GetResult();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                logger.LogInformation($"Finished in {elapsedTime}");
            }
        }
    }
}
