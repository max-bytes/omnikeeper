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
    public class GraphQLSchemaReloaderJob : IJob
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IModelContextBuilder modelContextBuilder;
        private readonly IServiceProvider sp;
        private readonly GraphQLSchemaHolder graphQLSchemaHolder;
        private readonly ILogger<GraphQLSchemaReloaderJob> logger;

        public GraphQLSchemaReloaderJob(ITraitsProvider traitsProvider, IModelContextBuilder modelContextBuilder, IServiceProvider sp,
            GraphQLSchemaHolder graphQLSchemaHolder, ILogger<GraphQLSchemaReloaderJob> logger)
        {
            this.traitsProvider = traitsProvider;
            this.modelContextBuilder = modelContextBuilder;
            this.sp = sp;
            this.graphQLSchemaHolder = graphQLSchemaHolder;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start");

                using (var trans = modelContextBuilder.BuildDeferred())
                {
                    // detect changes
                    var timeThreshold = TimeThreshold.BuildLatest();
                    var latestTraitChange = await traitsProvider.GetLatestChangeToActiveDataTraits(trans, timeThreshold);
                    if (latestTraitChange.HasValue)
                    {
                        var latestCreation = graphQLSchemaHolder.GetLatestSchemaCreation();

                        if (!latestCreation.HasValue || latestTraitChange.Value > latestCreation.Value)
                        { // reload
                            var activeTraits = await traitsProvider.GetActiveTraits(trans, timeThreshold);
                            graphQLSchemaHolder.ReInitSchema(sp, activeTraits, logger);
                        }
                    }
                }

                t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running graphql-schema-reloader job");
            }
        }
    }
}
