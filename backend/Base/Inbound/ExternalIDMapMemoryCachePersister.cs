using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public class ExternalIDMapMemoryCachePersister : IExternalIDMapPersister
    {
        private readonly IMemoryCache mc;
        private readonly string key;

        public ExternalIDMapMemoryCachePersister(IMemoryCache mc, string key)
        {
            this.mc = mc;
            this.key = key;
        }
        public async Task<IDictionary<Guid, string>> Load()
        {
            mc.TryGetValue(key, out var r);
            if (r is IDictionary<Guid, string> rr) return rr;
            return null;
        }

        public async Task Persist(IDictionary<Guid, string> int2ext)
        {
            mc.Set(key, int2ext);
        }
    }

}
