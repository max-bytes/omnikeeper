using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;

namespace Omnikeeper.Base.Service
{
    public static class CacheKeyService
    {
        public static string Attributes(Guid ciid, long layerID) => $"attributes_{ciid}_{layerID}";
        public static string CIIDsWithAttributeName(string attributeName, long layerID) => $"ciids_with_attribute_name_{attributeName}_{layerID}";

        public static string BaseConfiguration() => $"baseConfiguration";

        public static string Relations(IRelationSelection rs, long layerID) => $"relations_{rs.ToHashKey()}_{layerID}";

        public static string OIAConfig(string name) => $"oiaconfig_${name}";

        public static string ODataAPIContext(string id) => $"odataapicontext_${id}";

        public static string Predicates(AnchorStateFilter stateFilter) => $"predicates_{stateFilter}";

        public static string Predicate(string id) => $"predicate_{id}";

        public static string LatestPartitionIndex() => $"latestPartitionIndex";

        public static string Traits() => $"traits";

        public static string AllLayersByID() => $"layers_all_byid";
        public static string AllLayersByName() => $"layers_all_byname";
    }
}
