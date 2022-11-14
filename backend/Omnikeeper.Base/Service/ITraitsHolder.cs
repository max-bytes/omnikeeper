using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public interface ITraitsHolder
    {
        IDictionary<string, ITrait> GetTraits();

        void SetTraits(IDictionary<string, ITrait> traits, DateTimeOffset latestTraitsCreation, ILogger logger);

        DateTimeOffset? GetLatestTraitsCreation();
    }

    public static class TraitsHolderExtensions
    {
        public static IDictionary<string, ITrait> GetTraits(this ITraitsHolder traitsHolder, IEnumerable<string> traitIDs)
        {
            return traitsHolder.GetTraits().Where(kv => traitIDs.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        public static ITrait? GetTrait(this ITraitsHolder traitsHolder, string traitID)
        {
            var traits = traitsHolder.GetTraits();
            return traits.GetOrWithClass(traitID, null);
        }
    }
}
