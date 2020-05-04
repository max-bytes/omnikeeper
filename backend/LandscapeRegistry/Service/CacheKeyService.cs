using Landscape.Base.Entity;
using Landscape.Base.Utils;
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

        private static string CIChangeToken(Guid ciid) => $"ct_ci_{ciid}";
        public static CancellationChangeToken GetOrCreateCICancellationChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(CIChangeToken(ciid), (ce) => new CancellationTokenSource()).Token);
        public static void CancelCIChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            CancelAndRemoveChangeToken(memoryCache, CIChangeToken(ciid));

        private static string PredicatesChangeToken() => $"ct_predicates";
        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";
        public static CancellationChangeToken GetOrCreatePredicatesCancellationChangeToken(this IMemoryCache memoryCache) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(PredicatesChangeToken(), (ce) => new CancellationTokenSource()).Token);
        public static void CancelPredicatesChangeToken(this IMemoryCache memoryCache)
        {
            CancelAndRemoveChangeToken(memoryCache, PredicatesChangeToken());
        }

        private static void CancelAndRemoveChangeToken(IMemoryCache memoryCache, string tokenKey)
        {
            var token = memoryCache.Get<CancellationTokenSource>(tokenKey);
            if (token != null) // HACK Get() and Remove() are not atomic... is this a problem?
            {
                memoryCache.Remove(tokenKey);
                token.Cancel();
            }
        }
    }
}
