using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.CLB
{
    public class ValidatorProcessedChangesetsCache
    {
        private static IDictionary<string, IReadOnlyDictionary<string, Guid>> Cache = new Dictionary<string, IReadOnlyDictionary<string, Guid>>();

        public void UpdateCache(string contextID, IReadOnlyDictionary<string, Guid> latestChangesets)
        {
            Cache[contextID] = latestChangesets;
        }

        public void DeleteFromCache(string contextID)
        {
            Cache.Remove(contextID);
        }

        public IReadOnlyDictionary<string, Guid>? TryGetValue(string contextID)
        {
            if (Cache.TryGetValue(contextID, out var d))
                return d;
            return null;
        }
    }
}
