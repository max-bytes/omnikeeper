using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.CLB
{
    public class CLBProcessedChangesetsCache
    {
        private static IDictionary<string, IReadOnlyDictionary<string, Guid>> Cache = new Dictionary<string, IReadOnlyDictionary<string, Guid>>();

        public void UpdateCache(string clConfigID, string layerID, IReadOnlyDictionary<string, Guid> latestChangesets)
        {
            var key = $"{clConfigID}{layerID}";
            Cache[key] = latestChangesets;
        }

        public void DeleteFromCache(string clConfigID)
        {
            var keysToRemove = new HashSet<string>();
            foreach(var kv in Cache)
            {
                if (kv.Key.StartsWith(clConfigID))
                    keysToRemove.Add(kv.Key);
            }
            foreach (var key in keysToRemove)
                Cache.Remove(key);
        }

        public IReadOnlyDictionary<string, Guid>? TryGetValue(string clConfigID, string layerID)
        {
            var key = $"{clConfigID}{layerID}";
            if (Cache.TryGetValue(key, out var d))
                return d;
            return null;
        }
    }
}
