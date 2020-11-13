using Microsoft.Extensions.Caching.Memory;
using Omnikeeper.Base.Model;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Omnikeeper.Model
{
    public class MemoryCacheModel : IMemoryCacheModel
    {
        private IMemoryCache cache;

        public MemoryCacheModel(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public IEnumerable<string> GetKeys()
        {
            // have to get items by reflection
            var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field!.GetValue(cache);

            var keys = (value as IDictionary)!.Keys;

            var r = new List<string>();
            foreach (var key in keys)
                r.Add(key!.ToString()!);

            return r;
        }
    }
}
