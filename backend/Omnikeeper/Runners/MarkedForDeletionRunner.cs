using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using Omnikeeper.Utils;

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
                logger.LogInformation("Start");
                service.Run(modelContextBuilder, logger).GetAwaiter().GetResult();
                logger.LogInformation("Finished");
            }
        }
    }
}
