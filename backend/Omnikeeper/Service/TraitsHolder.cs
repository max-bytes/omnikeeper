using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL
{
    public class TraitsHolder : ITraitsHolder
    {
        private IDictionary<string, ITrait>? traits;
        private DateTimeOffset? latestTraitsCreation;

        private readonly object _lock = new();

        public IDictionary<string, ITrait> GetTraits()
        {
            lock (_lock)
            {
                if (traits == null)
                {
                    throw new Exception("Expected traits to be initialized before use");
                }
                return traits;
            }
        }

        public DateTimeOffset? GetLatestTraitsCreation() => latestTraitsCreation; // TODO: no locking required?

        public void SetTraits(IDictionary<string, ITrait> traits, DateTimeOffset latestTraitsCreation, ILogger logger)
        {
            logger.LogInformation("(Re-)initializing traits...");

            lock (_lock)
            {
                this.traits = traits;
                this.latestTraitsCreation = latestTraitsCreation;
            }

            logger.LogInformation("Finished initializing traits");
        }
    }
}
