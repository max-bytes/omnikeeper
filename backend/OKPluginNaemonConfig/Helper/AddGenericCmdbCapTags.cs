using System;
using System.Collections.Generic;
using System.Text;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void AddGenericCmdbCapTags(List<ConfigurationItem> ciData)
        {
            foreach (var ciItem in ciData)
            {
                if (ciItem.Categories.ContainsKey("MONITORING_CAP"))
                {
                    foreach (var category in ciItem.Categories["MONITORING_CAP"])
                    {
                        ciItem.Tags.Add($"cap_{category.Tree}_{category.Name}".ToLower());
                    }
                }
                else
                {
                    // TODO check if we should add this
                    //ciItem.Tags.Add("cap_default");
                }
            }
        }
    }
}
