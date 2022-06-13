using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.CLB
{
    public class ValidatorProcessedChangesetsCache
    {
        private static IDictionary<string, IDictionary<string, Guid>> Cache = new Dictionary<string, IDictionary<string, Guid>>();

        public void UpdateCache(string contextID, IDictionary<string, Guid> latestChangesets)
        {
            Cache[contextID] = latestChangesets;
        }

        public void DeleteFromCache(string contextID)
        {
            Cache.Remove(contextID);
        }

        public IDictionary<string, Guid>? TryGetValue(string contextID)
        {
            if (Cache.TryGetValue(contextID, out var d))
                return d;
            return null;
        }
    }
}
