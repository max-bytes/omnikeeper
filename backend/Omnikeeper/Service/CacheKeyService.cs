using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace Omnikeeper.Service
{
    public static class CacheKeyService
    {
        public static string Attributes(Guid ciid, long layerID) => $"attributes_{ciid}_{layerID}";
        private static string AttributesChangeToken(Guid ciid, long layerID) => $"ct_att_{ciid}_{layerID}";
        public static CancellationChangeToken GetAttributesCancellationChangeToken(this IMemoryCache memoryCache, Guid ciid, long layerID) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(AttributesChangeToken(ciid, layerID), (ce) => new CancellationTokenSource()).Token);
        public static void CancelAttributesChangeToken(this IMemoryCache memoryCache, Guid ciid, long layerID) =>
            CancelAndRemoveChangeToken(memoryCache, AttributesChangeToken(ciid, layerID));

        public static string Relations(IRelationSelection rs, long layerID) => $"relations_{rs.ToHashKey()}_{layerID}";
        private static string RelationsChangeToken(IRelationSelection rs, long layerID) => $"ct_rel_{rs.ToHashKey()}_{layerID}";
        public static CancellationChangeToken GetRelationsCancellationChangeToken(this IMemoryCache memoryCache, IRelationSelection rs, long layerID) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(RelationsChangeToken(rs, layerID), (ce) => new CancellationTokenSource()).Token);
        public static void CancelRelationsChangeToken(this IMemoryCache memoryCache, IRelationSelection rs, long layerID) =>
            CancelAndRemoveChangeToken(memoryCache, RelationsChangeToken(rs, layerID));

        internal static object OIAConfig(string name) => $"oiaconfig_${name}";
        private static string OIAConfigChangeToken(string name) => $"ct_oiaconfig_{name}";
        public static CancellationChangeToken GetOIAConfigCancellationChangeToken(this IMemoryCache memoryCache, string name) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(OIAConfigChangeToken(name), (ce) => new CancellationTokenSource()).Token);
        public static void CancelOIAConfigChangeToken(this IMemoryCache memoryCache, string name) =>
            CancelAndRemoveChangeToken(memoryCache, OIAConfigChangeToken(name));


        internal static object ODataAPIContext(string id) => $"odataapicontext_${id}";
        private static string ODataAPIContextChangeToken(string id) => $"ct_odataapicontext_{id}";
        public static CancellationChangeToken GetODataAPIContextCancellationChangeToken(this IMemoryCache memoryCache, string id) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(ODataAPIContextChangeToken(id), (ce) => new CancellationTokenSource()).Token);
        public static void CancelODataAPIContextChangeToken(this IMemoryCache memoryCache, string id) =>
            CancelAndRemoveChangeToken(memoryCache, ODataAPIContextChangeToken(id));

        private static string PredicatesChangeToken() => $"ct_predicates";
        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";
        public static CancellationChangeToken GetPredicatesCancellationChangeToken(this IMemoryCache memoryCache) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(PredicatesChangeToken(), (ce) => new CancellationTokenSource()).Token);
        public static void CancelPredicatesChangeToken(this IMemoryCache memoryCache) =>
            CancelAndRemoveChangeToken(memoryCache, PredicatesChangeToken());

        private static string PredicateChangeToken(string id) => $"predicate_{id}";
        public static string Predicate(string id) => $"predicate_{id}";
        public static CancellationChangeToken GetPredicateCancellationToken(this IMemoryCache memoryCache, string id) => 
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(PredicateChangeToken(id), (ce) => new CancellationTokenSource()).Token);
        public static void CancelPredicateChangeToken(this IMemoryCache memoryCache, string id) =>
               CancelAndRemoveChangeToken(memoryCache, PredicateChangeToken(id));

        private static string TraitsChangeToken() => $"ct_traits";
        public static string Traits() => $"traits";
        public static CancellationChangeToken GetTraitsCancellationChangeToken(this IMemoryCache memoryCache) =>
            new CancellationChangeToken(memoryCache.GetOrCreateThreadSafe(TraitsChangeToken(), (ce) => new CancellationTokenSource()).Token);
        public static void CancelTraitsChangeToken(this IMemoryCache memoryCache) =>
            CancelAndRemoveChangeToken(memoryCache, TraitsChangeToken());


        public static string LayerSet(string[] layerNames) => $"layerSet_{string.Join(',', layerNames)}";
        public static string AllLayersSet() => $"layerSet_all";
        public static string AllLayers() => $"layers_all";
        public static string LayerById(long layerID) => $"layerByID_{layerID}";
        public static string LayerByName(string layerName) => $"layerByName_{layerName}";
        public static string LayersByIDs(long[] layerIDs) => $"layersByIDs_{string.Join(',', layerIDs)}";
        public static string LayersByStateFilter(AnchorStateFilter stateFilter) => $"layersByStateFilter_{stateFilter}";
        private static string LayersChangeToken() => $"ct_layers";
        // NOTE: the following functions inclue logging while the others don't
        // this was/is to allow debugging of caching issues in general
        // we'll leave these as is
        public static CancellationChangeToken GetLayersCancellationChangeToken(this IMemoryCache memoryCache, ILogger logger)
        {
            logger.LogDebug("Getting CancellationTokenSource");
            var ts = memoryCache.GetOrCreateThreadSafe(LayersChangeToken(), (ce) =>
            {
                var ts = new CancellationTokenSource(); ;
                logger.LogDebug($"create new cancellation token source for Layers: {ts.GetHashCode()}");
                return ts;
            });
            logger.LogDebug($"Got CancellationTokenSource: {ts.GetHashCode()}");

            var t = new CancellationChangeToken(ts.Token);
            logger.LogDebug($"Returning Layer Cancellation Change Token");
            return t;
        }
        public static void CancelLayersChangeTokens(this IMemoryCache memoryCache, ILogger logger)
        {
            logger.LogDebug("Getting Layers Cancellation Change Token");
            CancelAndRemoveChangeToken(memoryCache, LayersChangeToken(), logger);
        }


        private static void CancelAndRemoveChangeToken(IMemoryCache memoryCache, string tokenKey, ILogger logger = null)
        {
            if (logger != null)
                logger.LogDebug($"Cancelling the token source with key {tokenKey}");
            lock (TypeLock<CancellationTokenSource>.Lock)  // Get() and Remove() are not atomic, so we add a lock here
            {
                var tokenSource = memoryCache.Get<CancellationTokenSource>(tokenKey);
                if (tokenSource != null)
                {
                    if (logger != null)
                        logger.LogDebug($"Cancelling the token source with token {tokenSource.GetHashCode()}");
                    memoryCache.Remove(tokenKey);
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                else
                {
                    if (logger != null)
                        logger.LogDebug($"Could not find token source with key {tokenKey}");
                }
            }
        }

        /// <summary>
        /// despite some conflicting information on the web, IMemoryCache.GetOrCreate() is NOT(!) properly thread safe. That means that parallel invocations of it can lead to different values getting returned
        /// this function fixes the issue by using a lock
        /// see https://github.com/aspnet/Caching/issues/359 and https://github.com/dotnet/runtime/issues/36499 for the issue
        /// see https://tpodolak.com/blog/2017/12/13/asp-net-core-memorycache-getorcreate-calls-factory-method-multiple-times/ for the base solution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memoryCache"></param>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        private static T GetOrCreateThreadSafe<T>(this IMemoryCache memoryCache, object key, Func<ICacheEntry, T> factory)
        {
            // try to avoid lock by first checking if the key is already present (which should be the case most of the time)
            if (memoryCache.TryGetValue<T>(key, out var result))
            {
                return result;
            }

            lock (TypeLock<T>.Lock)
            {
                return memoryCache.GetOrCreate<T>(key, factory);
            }

        }

        private static class TypeLock<T>
        {
            public static object Lock { get; } = new object();
        }

    }
}
