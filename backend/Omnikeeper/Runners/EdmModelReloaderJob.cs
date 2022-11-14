using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils;
using Omnikeeper.Controllers.OData;
using Omnikeeper.GraphQL;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class EdmModelReloaderJob : IJob
    {
        private readonly ITraitsHolder traitsHolder;
        private readonly IServiceProvider sp;
        private readonly EdmModelHolder edmModelHolder;
        private readonly ILogger<EdmModelReloaderJob> logger;

        public EdmModelReloaderJob(ITraitsHolder traitsHolder, IServiceProvider sp,
            EdmModelHolder edmModelHolder, ILogger<EdmModelReloaderJob> logger)
        {
            this.traitsHolder = traitsHolder;
            this.sp = sp;
            this.edmModelHolder = edmModelHolder;
            this.logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start");

                // detect changes
                var latestTraitChange = traitsHolder.GetLatestTraitsCreation();
                if (latestTraitChange.HasValue)
                {
                    var latestCreation = edmModelHolder.GetLatestModelCreation();

                    if (!latestCreation.HasValue || latestTraitChange.Value > latestCreation.Value)
                    { // reload
                        var activeTraits = traitsHolder.GetTraits();
                        edmModelHolder.ReInitModel(sp, activeTraits, logger);
                    }
                }

                t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running edm-model-reloader job");
            }

            return Task.CompletedTask;
        }
    }
}
