using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Omnikeeper.Base.Model;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Omnikeeper.Model
{
    public class CacheModel : ICacheModel
    {
        private IDistributedCache cache;

        public CacheModel(IDistributedCache cache)
        {
            this.cache = cache;
        }

        public IEnumerable<string> GetKeys()
        {
            // have to get items by reflection
            // TODO: move and encapsulate access to IDistributedCache, maybe use DistributedCacheExtensions
            var memcacheField = typeof(MemoryDistributedCache).GetField("_memCache", BindingFlags.NonPublic | BindingFlags.Instance);
            var memcache = (MemoryCache)memcacheField!.GetValue(cache)!;
            var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field!.GetValue(memcache);

            var keys = (value as IDictionary)!.Keys;

            var r = new List<string>();
            foreach (var key in keys)
                r.Add(key!.ToString()!);

            return r;
        }
    }
}
