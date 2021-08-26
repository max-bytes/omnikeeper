﻿using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Validation;
using Omnikeeper.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Omnikeeper.Validation
{
    public class ValidationEngineRunner
    {
        private readonly IValidationEngine validationEngine;
        private readonly IServiceProvider sp;
        private readonly ILogger<ValidationEngineRunner> logger;

        public ValidationEngineRunner(IValidationEngine validationEngine, IServiceProvider sp, ILogger<ValidationEngineRunner> logger)
            {
            this.validationEngine = validationEngine;
            this.sp = sp;
            this.logger = logger;
        }

        [MaximumConcurrentExecutions(1, timeoutInSeconds: 120)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        public void Run(PerformContext? context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                RunAsync().GetAwaiter().GetResult();
            }
        }

        public async Task RunAsync()
        {
            logger.LogInformation("Start");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            await validationEngine.Run(logger);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            logger.LogInformation($"Done in {elapsedTime}");
        }
    }
}
