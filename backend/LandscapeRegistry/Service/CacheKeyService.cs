using Landscape.Base.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    public static class CacheKeyService
    {
        public static string EffectiveTraitsOfCI(MergedCI ci) => $"effectiveTraitsOfCI_{ci.ID}_{ci.Layers.LayerHash}";

        private static string CIChangeToken(Guid ciid) => $"ci_{ciid}";
        public static CancellationChangeToken GetOrCreateCICancellationChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(CIChangeToken(ciid), (ce) => new CancellationTokenSource()).Token);
        public static void CancelCIChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            memoryCache.Get<CancellationTokenSource>(CIChangeToken(ciid))?.Cancel();
    }
}
