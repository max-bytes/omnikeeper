using DotLiquid.Util;
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

        public static string CIOnLayer(Guid ciid, long layerID) => $"ciOnLayer_{ciid}_{layerID}";

        public static string MergedCI(Guid ciid, LayerSet layers) => $"mergedCI_{ciid}_{layers.LayerHash}";
        private static string CIChangeToken(Guid ciid) => $"ct_ci_{ciid}";
        public static CancellationChangeToken GetCICancellationChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(CIChangeToken(ciid), (ce) => new CancellationTokenSource()).Token);
        public static void CancelCIChangeToken(this IMemoryCache memoryCache, Guid ciid) =>
            CancelAndRemoveChangeToken(memoryCache, CIChangeToken(ciid));

        private static string PredicatesChangeToken() => $"ct_predicates";
        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";
        public static CancellationChangeToken GetPredicatesCancellationChangeToken(this IMemoryCache memoryCache) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(PredicatesChangeToken(), (ce) => new CancellationTokenSource()).Token);
        public static void CancelPredicatesChangeToken(this IMemoryCache memoryCache) =>
            CancelAndRemoveChangeToken(memoryCache, PredicatesChangeToken());

        public static string LayerSet(string[] layerNames) => $"layerSet_{string.Join(',', layerNames)}";
        public static string AllLayersSet() => $"layerSet_all";
        public static string AllLayers() => $"layers_all";
        public static string LayerById(long layerID) => $"layerByID_{layerID}";
        public static string LayerByName(string layerName) => $"layerByName_{layerName}";
        public static string LayersByIDs(long[] layerIDs) => $"layersByIDs_{string.Join(',', layerIDs)}";
        public static string LayersByStateFilter(AnchorStateFilter stateFilter) => $"layersByStateFilter_{stateFilter}";
        private static string LayersChangeToken() => $"ct_layers";
        public static CancellationChangeToken GetLayersCancellationChangeToken(this IMemoryCache memoryCache) =>
            new CancellationChangeToken(memoryCache.GetOrCreate(LayersChangeToken(), (ce) => new CancellationTokenSource()).Token);
        public static void CancelLayersChangeTokens(this IMemoryCache memoryCache) =>
            CancelAndRemoveChangeToken(memoryCache, LayersChangeToken());


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
