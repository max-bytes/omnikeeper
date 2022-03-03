using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Omnikeeper.Base.CLB
{
    public class CLBLastRunCache
    {
        private readonly IDictionary<string, DateTimeOffset> cache = new Dictionary<string, DateTimeOffset>();

        public void UpdateCache(string key, DateTimeOffset latestChange)
        {
            cache[key] = latestChange;
        }

        public void RemoveFromCache(string key)
        {
            cache.Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out DateTimeOffset v)
        {
            return cache.TryGetValue(key, out v);
        }
    }
}
