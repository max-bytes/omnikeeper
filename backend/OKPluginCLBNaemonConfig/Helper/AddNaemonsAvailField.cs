using System.Collections.Generic;
using System.Linq;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void AddNaemonsAvailField(
            List<ConfigurationItem> ciData,
            List<string> naemonIds,
            Dictionary<string, List<string>> capMap
            )
        {
            foreach (var item in ciData)
            {
                // TODO how to handle cases that dont have tags should NaemonsAvail include all naemon ids
                item.NaemonsAvail = naemonIds;

                foreach (var requirement in item.Tags)
                {
                    if (capMap.ContainsKey(requirement))
                    {
                        item.NaemonsAvail = item.NaemonsAvail.Intersect(capMap[requirement]).ToList();
                    }
                    else
                    {
                        item.NaemonsAvail = new List<string>();
                    }
                }
            }
        }
    }
}
