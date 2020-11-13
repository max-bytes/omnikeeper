using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;
using System.Threading;

namespace Omnikeeper.Base.Service
{
    public static class CacheKeyService
    {
        public static string Attributes(Guid ciid, long layerID) => $"attributes_{ciid}_{layerID}";
        public static string AttributesChangeToken(Guid ciid, long layerID) => $"ct_att_{ciid}_{layerID}";

        public static string BaseConfiguration() => $"baseConfiguration";
        public static string BaseConfigurationChangeToken() => $"ct_baseConfiguration";

        public static string Relations(IRelationSelection rs, long layerID) => $"relations_{rs.ToHashKey()}_{layerID}";
        public static string RelationsChangeToken(IRelationSelection rs, long layerID) => $"ct_rel_{rs.ToHashKey()}_{layerID}";

        public static string OIAConfig(string name) => $"oiaconfig_${name}";
        public static string OIAConfigChangeToken(string name) => $"ct_oiaconfig_{name}";


        public static string ODataAPIContext(string id) => $"odataapicontext_${id}";
        public static string ODataAPIContextChangeToken(string id) => $"ct_odataapicontext_{id}";

        public static string PredicatesChangeToken() => $"ct_predicates";
        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";

        public static string PredicateChangeToken(string id) => $"predicate_{id}";
        public static string Predicate(string id) => $"predicate_{id}";

        public static string TraitsChangeToken() => $"ct_traits";
        public static string Traits() => $"traits";


        public static string LayerSet(string[] layerNames) => $"layerSet_{string.Join(',', layerNames)}";
        public static string AllLayersSet() => $"layerSet_all";
        public static string AllLayers() => $"layers_all";
        public static string LayerById(long layerID) => $"layerByID_{layerID}";
        public static string LayerByName(string layerName) => $"layerByName_{layerName}";
        public static string LayersByIDs(long[] layerIDs) => $"layersByIDs_{string.Join(',', layerIDs)}";
        public static string LayersByStateFilter(AnchorStateFilter stateFilter) => $"layersByStateFilter_{stateFilter}";
        public static string LayersChangeToken() => $"ct_layers";
        // NOTE: the following functions inclue logging while the others don't
        // this was/is to allow debugging of caching issues in general
        // we'll leave these as is
        //public static CancellationChangeToken GetLayersCancellationChangeToken(this IMemoryCache memoryCache, ILogger logger)
        //{
        //    logger.LogDebug("Getting CancellationTokenSource");
        //    var ts = memoryCache.GetOrCreateThreadSafe(LayersChangeToken(), (ce) =>
        //    {
        //        var ts = new CancellationTokenSource(); ;
        //        logger.LogDebug($"create new cancellation token source for Layers: {ts.GetHashCode()}");
        //        return ts;
        //    });
        //    logger.LogDebug($"Got CancellationTokenSource: {ts.GetHashCode()}");

        //    var t = new CancellationChangeToken(ts.Token);
        //    logger.LogDebug($"Returning Layer Cancellation Change Token");
        //    return t;
        //}
        //public static void CancelLayersChangeTokens(this IMemoryCache memoryCache, ILogger logger)
        //{
        //    logger.LogDebug("Getting Layers Cancellation Change Token");
        //    CancelAndRemoveChangeToken(memoryCache, LayersChangeToken(), logger);
        //}

        //public static void AddCancellationTokenToCacheEntry(this IMemoryCache memoryCache, ICacheEntry ce, string tokenName, ILogger logger)
        //{
        //    ce?.AddExpirationToken(GetCancellationChangeToken(memoryCache, tokenName, logger));
        //}
    }
}
