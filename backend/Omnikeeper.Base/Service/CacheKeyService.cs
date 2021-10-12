using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;

namespace Omnikeeper.Base.Service
{
    public static class CacheKeyService
    {
        public static string MetaConfiguration() => $"metaConfiguration";

        public static string BaseConfiguration() => $"baseConfiguration";

        public static string OIAConfig(string name) => $"oiaconfig_${name}";

        public static string ODataAPIContext(string id) => $"odataapicontext_${id}";

        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";

        public static string Predicate(string id) => $"predicate_{id}";

        public static string LatestPartitionIndex() => $"latestPartitionIndex";

        public static string Traits() => $"traits";

        public static string AllLayersByID() => $"layers_all_byid";

        public static string CINames(string layerID) => $"cinames_{layerID}";
    }
}
