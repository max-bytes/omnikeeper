using LandscapeRegistry.Service;
using Microsoft.Extensions.Logging;

namespace LandscapeRegistry.Runners
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

        public void Run()
        {
            logger.LogInformation("Start");
            service.Run(logger).GetAwaiter().GetResult();
            logger.LogInformation("Finished");
        }
    }
}
