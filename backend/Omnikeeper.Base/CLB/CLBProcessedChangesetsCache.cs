using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public class CLBProcessedChangesetsCache
    {
        private static IDictionary<string, IDictionary<string, Guid>> Cache = new Dictionary<string, IDictionary<string, Guid>>();

        public void UpdateCache(string clConfigID, string layerID, IDictionary<string, Guid> latestChangesets)
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

        public IDictionary<string, Guid>? TryGetValue(string clConfigID, string layerID)
        {
            var key = $"{clConfigID}{layerID}";
            if (Cache.TryGetValue(key, out var d))
                return d;
            return null;
        }
    }
}
