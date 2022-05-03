using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void UpdateLocationField(List<ConfigurationItem> ciData)
        {
            foreach (var ciItem in ciData)
            {
                if (ciItem.Vars.ContainsKey("LOCATION"))
                {
                    if (ciItem.Vars["LOCATION"] == "")
                    {
                        if (ciItem.Relations.ContainsKey("OUT"))
                        {
                            if (ciItem.Relations["OUT"].ContainsKey("runs_on"))
                            {
                                if (ciItem.Relations["OUT"]["runs_on"].Count > 0)
                                {
                                    var targetCIID = ciItem.Relations["OUT"]["runs_on"][0];
                                    var targetCI = ciData.Where(c => c.Id == targetCIID).FirstOrDefault();

                                    if (targetCI != null)
                                    {
                                        ciItem.Vars["LOCATION"] = targetCI.Vars["LOCATION"];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void UpdateLocationField(Dictionary<string, ConfigurationItem> ciData)
        {
            foreach (var (_,ciItem) in ciData)
            {
                if (ciItem.Vars.ContainsKey("LOCATION"))
                {
                    if (ciItem.Vars["LOCATION"] == "")
                    {
                        if (ciItem.Relations.ContainsKey("OUT"))
                        {
                            if (ciItem.Relations["OUT"].ContainsKey("runs_on"))
                            {
                                if (ciItem.Relations["OUT"]["runs_on"].Count > 0)
                                {
                                    var targetCIID = ciItem.Relations["OUT"]["runs_on"][0];
                                    //var targetCI = ciData.Where(c => c.Id == targetCIID).FirstOrDefault();

                                    // NOTE we need to fix this case
                                    //var targetCI = ciData[targetCIID];

                                    //if (targetCI != null)
                                    //{
                                    //    ciItem.Vars["LOCATION"] = targetCI.Vars["LOCATION"];
                                    //}
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
