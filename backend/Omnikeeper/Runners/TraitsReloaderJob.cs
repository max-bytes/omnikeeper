using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GraphQL;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class TraitsReloaderJob : IJob
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly ITraitsHolder traitsHolder;
        private readonly ILogger<GraphQLSchemaReloaderJob> logger;

        public TraitsReloaderJob(ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder, ITraitsHolder traitsHolder, ILogger<GraphQLSchemaReloaderJob> logger)
        {
            this.traitsProvider = traitsProvider;
            this.modelContextBuilder = modelContextBuilder;
            this.traitsHolder = traitsHolder;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start traits-reloader job");

                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    // detect changes
                    var timeThreshold = TimeThreshold.BuildLatest();
                    var latestTraitChange = await traitsProvider.GetLatestChangeToActiveDataTraits(trans, timeThreshold);
                    if (latestTraitChange.HasValue)
                    {
                        var latestCreation = traitsHolder.GetLatestTraitsCreation();

                        if (!latestCreation.HasValue || latestTraitChange.Value > latestCreation.Value)
                        { // reload
                            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);
                            traitsHolder.SetTraits(activeTraits, latestTraitChange.Value, logger);
                        }
                    }
                }

                t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running traits-reloader job");
            }
        }
    }
}
