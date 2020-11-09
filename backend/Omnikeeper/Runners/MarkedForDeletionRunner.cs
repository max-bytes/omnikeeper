using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Omnikeeper.Service;
using Omnikeeper.Utils;

namespace Omnikeeper.Runners
{
    public class MarkedForDeletionRunner
    {
        private readonly MarkedForDeletionService service;
        private readonly ILogger<MarkedForDeletionRunner> logger;

        public MarkedForDeletionRunner(MarkedForDeletionService service, ILogger<MarkedForDeletionRunner> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        // TODO: enable and test
        //[DisableConcurrentExecution(timeoutInSeconds: 60)]
        //[AutomaticRetry(Attempts = 0)]
        public void Run(PerformContext context)
        {
            using (HangfireConsoleLogger.InContext(context))
            {
                logger.LogInformation("Start");
                service.Run(logger).GetAwaiter().GetResult();
                logger.LogInformation("Finished");
            }
        }
    }
}
