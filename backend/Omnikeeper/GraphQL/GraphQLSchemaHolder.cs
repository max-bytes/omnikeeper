using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.GraphQL.TraitEntities;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL
{
    public class GraphQLSchemaHolder
    {
        private ISchema? schema;
        private DateTimeOffset? latestSchemaCreation;

        private readonly object _lock = new();

        public ISchema GetSchema()
        {
            lock (_lock)
            {
                if (schema == null)
                {
                    throw new Exception("Expected schema to be initialized before use");
                }
                return schema;
            }
        }

        public DateTimeOffset? GetLatestSchemaCreation() => latestSchemaCreation;

        public void ReInitSchema(IServiceProvider sp, IDictionary<string, ITrait> activeTraits, ILogger logger)
        {
            var typeContainerCreator = sp.GetRequiredService<TypeContainerCreator>();
            var traitEntitiesQuerySchemaLoader = sp.GetRequiredService<TraitEntitiesQuerySchemaLoader>();
            var traitEntitiesMutationSchemaLoader = sp.GetRequiredService<TraitEntitiesMutationSchemaLoader>();

            logger.LogInformation("(Re-)initializing GraphQL schema...");

            lock (_lock)
            {
                schema = new GraphQLSchema(sp);

                try
                {
                    var tet = sp.GetRequiredService<TraitEntitiesType>();
                    var mergedCIType = sp.GetRequiredService<MergedCIType>();
                    var mutation = sp.GetRequiredService<GraphQLMutation>();
                    var typeContainer = typeContainerCreator.CreateTypes(activeTraits, logger);
                    traitEntitiesQuerySchemaLoader.Init(mergedCIType, tet, typeContainer);
                    traitEntitiesMutationSchemaLoader.Init(mutation, typeContainer);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Encountered error while creating trait entity GraphQL schema");
                }

                // we force a schema initialization here, so it does not need to be done at request time anymore
                schema.Initialize();

                latestSchemaCreation = DateTimeOffset.Now;
            }

            logger.LogInformation("Finished initializing GraphQL scheme");
        }
    }
}
