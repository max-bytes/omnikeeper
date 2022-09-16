using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Omnikeeper.Base.CLB
{
    public class CLBProcessingCache
    {
        private static IDictionary<string, (IReadOnlyDictionary<string, Guid> processedChangesets, DateTimeOffset configActuality)> Cache = new ConcurrentDictionary<string, (IReadOnlyDictionary<string, Guid> processedChangesets, DateTimeOffset configActuality)>();

        public void UpdateCache(string clConfigID, string layerID, IReadOnlyDictionary<string, Guid> processedChangesets, DateTimeOffset configActuality)
        {
            var key = $"{clConfigID}{layerID}";
            Cache[key] = (processedChangesets, configActuality);
        }

        //public void DeleteFromCache(string clConfigID)
        //{
        //    var keysToRemove = new HashSet<string>();
        //    foreach(var kv in Cache)
        //    {
        //        if (kv.Key.StartsWith(clConfigID))
        //            keysToRemove.Add(kv.Key);
        //    }
        //    foreach (var key in keysToRemove)
        //        Cache.Remove(key);
        //}

        public (IReadOnlyDictionary<string, Guid>? processedChangesets, DateTimeOffset? configActuality) TryGetValue(string clConfigID, string layerID)
        {
            var key = $"{clConfigID}{layerID}";
            if (Cache.TryGetValue(key, out var d))
                return d;
            return (null, null);
        }
    }
}
