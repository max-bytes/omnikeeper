using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    public class ExternalIDMapMemoryCachePersister : IExternalIDMapPersister
    {
        private readonly IMemoryCache mc;

        public ExternalIDMapMemoryCachePersister(IMemoryCache mc)
        {
            this.mc = mc;
        }
        public async Task<IDictionary<Guid, string>> Load(string scope)
        {
            mc.TryGetValue(scope, out var r);
            if (r is IDictionary<Guid, string> rr)
            {
                return rr;
            }
            return null;
        }

        public async Task Persist(string scope, IDictionary<Guid, string> int2ext)
        {
            mc.Set(scope, int2ext);
        }
    }

}
