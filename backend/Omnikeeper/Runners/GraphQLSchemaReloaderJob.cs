using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Runners
{
    [DisallowConcurrentExecution]
    public class GraphQLSchemaReloaderJob : IJob
    {
        private readonly ITraitsHolder traitsHolder;
        private readonly IServiceProvider sp;
        private readonly GraphQLSchemaHolder graphQLSchemaHolder;
        private readonly ILogger<GraphQLSchemaReloaderJob> logger;

        public GraphQLSchemaReloaderJob(ITraitsHolder traitsHolder, IServiceProvider sp, GraphQLSchemaHolder graphQLSchemaHolder, ILogger<GraphQLSchemaReloaderJob> logger)
        {
            this.traitsHolder = traitsHolder;
            this.sp = sp;
            this.graphQLSchemaHolder = graphQLSchemaHolder;
            this.logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var t = new StopTimer();
                logger.LogTrace("Start");

                var latestTraitChange = traitsHolder.GetLatestTraitsCreation();
                if (latestTraitChange.HasValue)
                {
                    var latestCreation = graphQLSchemaHolder.GetLatestSchemaCreation();

                    if (!latestCreation.HasValue || latestTraitChange.Value > latestCreation.Value)
                    { // reload
                        var activeTraits = traitsHolder.GetTraits();
                        graphQLSchemaHolder.ReInitSchema(sp, activeTraits, logger);
                    }
                }

                t.Stop((ts, elapsedTime) => logger.LogTrace($"Finished in {elapsedTime}"));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running graphql-schema-reloader job");
            }

            return Task.CompletedTask;
        }
    }
}
