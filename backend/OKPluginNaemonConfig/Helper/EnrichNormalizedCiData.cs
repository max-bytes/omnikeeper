using OKPluginNaemonConfig.Entity;
using System.Collections.Generic;
using System.Linq;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static Dictionary<string, List<string>> BuildCapMap(
            IDictionary<string, NaemonInstancesTag> naemonInstancesTags, 
            IDictionary<string, NaemonProfile> naemonProfiles,
            IDictionary<string, NaemonInstance> naemonInstances,
            List<string> naemonsConfigGenerateprofiles
            )
        {
            var capMap = new Dictionary<string, List<string>>();

            foreach (var ciItem in naemonInstancesTags)
            {
                if (ciItem.Value.Tag.StartsWith("cap_"))
                {
                    if (capMap.ContainsKey(ciItem.Value.Tag))
                    {
                        capMap[ciItem.Value.Tag].Add(ciItem.Value.Id);
                    }
                    else
                    {
                        capMap.Add(ciItem.Value.Tag, new List<string> { ciItem.Value.Id });
                    }
                }
            }


            var profileFromDbNaemons = new List<string>();

            foreach (var ciItem in naemonInstances)
            {
                // we need to check here if isNaemonProfileFromDbEnabled 

                if (naemonsConfigGenerateprofiles.Contains(ciItem.Value.Name))
                {
                    profileFromDbNaemons.Add(ciItem.Value.Id);
                }
            }

            /* extend capMap */
            if (profileFromDbNaemons.Count > 0)
            {
                foreach (var ciItem in naemonProfiles)
                {
                    var cap = $"cap_lp_{ciItem.Value.Name}";
                    if (!capMap.ContainsKey(cap))
                    {
                        capMap.Add(cap, profileFromDbNaemons);
                    }
                    else
                    {
                        capMap[cap] = (List<string>)profileFromDbNaemons.Concat(capMap[cap]);
                    }
                }
            }

            return capMap;
        }
    }
}
