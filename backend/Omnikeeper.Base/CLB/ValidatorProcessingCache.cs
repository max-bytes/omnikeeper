using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Omnikeeper.Base.CLB
{
    public class ValidatorProcessingCache
    {
        private static readonly IDictionary<string, (IReadOnlyDictionary<string, Guid> processedChangesets, DateTimeOffset contextActuality)> cache = new ConcurrentDictionary<string, (IReadOnlyDictionary<string, Guid> processedChangesets, DateTimeOffset contextActuality)>();

        public void UpdateProcessedChangesets(string contextID, IReadOnlyDictionary<string, Guid> latestChangesets, DateTimeOffset contextActuality)
        {
            cache[contextID] = (latestChangesets, contextActuality);
        }

        //public void DeleteContext(string contextID)
        //{
        //    cache.Remove(contextID);
        //}

        public (IReadOnlyDictionary<string, Guid>? processedChangesets, DateTimeOffset? contextActuality) TryGetProcessedChangesets(string contextID)
        {
            if (cache.TryGetValue(contextID, out var d))
                return d;
            return (null, null);
        }
    }
}
