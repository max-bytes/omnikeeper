using System.Collections.Generic;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void AddNaemonVars(List<ConfigurationItem> ciData)
        {
            // general vars

            foreach (var ciItem in ciData)
            {
                if (!ciItem.Vars.ContainsKey("LOCATION"))
                {
                    ciItem.Vars.Add("LOCATION", ciItem.Location);
                }
                else
                {
                    ciItem.Vars["LOCATION"] = ciItem.Location;
                }

                if (!ciItem.Vars.ContainsKey("OS"))
                {
                    ciItem.Vars.Add("OS", ciItem.OS);
                }
                else
                {
                    ciItem.Vars["OS"] = ciItem.OS;
                }

                if (!ciItem.Vars.ContainsKey("PLATFORM"))
                {
                    ciItem.Vars.Add("PLATFORM", ciItem.Platform);
                }
                else
                {
                    ciItem.Vars["PLATFORM"] = ciItem.Platform;
                }

                if (!ciItem.Vars.ContainsKey("ADDRESS"))
                {
                    ciItem.Vars.Add("ADDRESS", ciItem.Address);
                }
                else
                {
                    ciItem.Vars["ADDRESS"] = ciItem.Address;
                }

                string p = string.Empty;

                if (ciItem.Port != null)
                    p = ciItem.Port.ToString()!;

                if (!ciItem.Vars.ContainsKey("PORT"))
                {
                    ciItem.Vars.Add("PORT", p);
                }
                else
                {
                    ciItem.Vars["PORT"] = p;
                }

                // add fkey and other missing vars
                if (!ciItem.Vars.ContainsKey("FKEY"))
                {
                    ciItem.Vars.Add("FKEY", ciItem.FKey);
                }
                else
                {
                    ciItem.Vars["FKEY"] = ciItem.FKey;
                }

                if (!ciItem.Vars.ContainsKey("FSOURCE"))
                {
                    ciItem.Vars.Add("FSOURCE", ciItem.FSource);
                }
                else
                {
                    ciItem.Vars["FSOURCE"] = ciItem.FSource;
                }

                if (!ciItem.Vars.ContainsKey("INSTANCE"))
                {
                    ciItem.Vars.Add("INSTANCE", ciItem.Instance);
                }
                else
                {
                    ciItem.Vars["INSTANCE"] = ciItem.Instance;
                }

                // Oracle

            }
        }

        public static void AddNaemonVars(Dictionary<string, ConfigurationItem> ciData)
        {
            // general vars

            foreach (var (_, ciItem) in ciData)
            {
                if (!ciItem.Vars.ContainsKey("LOCATION"))
                {
                    ciItem.Vars.Add("LOCATION", ciItem.Location);
                }
                else
                {
                    ciItem.Vars["LOCATION"] = ciItem.Location;
                }

                if (!ciItem.Vars.ContainsKey("OS"))
                {
                    ciItem.Vars.Add("OS", ciItem.OS);
                }
                else
                {
                    ciItem.Vars["OS"] = ciItem.OS;
                }

                if (!ciItem.Vars.ContainsKey("PLATFORM"))
                {
                    ciItem.Vars.Add("PLATFORM", ciItem.Platform);
                }
                else
                {
                    ciItem.Vars["PLATFORM"] = ciItem.Platform;
                }

                if (!ciItem.Vars.ContainsKey("ADDRESS"))
                {
                    ciItem.Vars.Add("ADDRESS", ciItem.Address);
                }
                else
                {
                    ciItem.Vars["ADDRESS"] = ciItem.Address;
                }

                string p = string.Empty;

                if (ciItem.Port != null)
                    p = ciItem.Port.ToString()!;

                if (!ciItem.Vars.ContainsKey("PORT"))
                {
                    ciItem.Vars.Add("PORT", p);
                }
                else
                {
                    ciItem.Vars["PORT"] = p;
                }

                // add fkey and other missing vars
                if (!ciItem.Vars.ContainsKey("FKEY"))
                {
                    ciItem.Vars.Add("FKEY", ciItem.FKey);
                }
                else
                {
                    ciItem.Vars["FKEY"] = ciItem.FKey;
                }

                if (!ciItem.Vars.ContainsKey("FSOURCE"))
                {
                    ciItem.Vars.Add("FSOURCE", ciItem.FSource);
                }
                else
                {
                    ciItem.Vars["FSOURCE"] = ciItem.FSource;
                }

                if (!ciItem.Vars.ContainsKey("INSTANCE"))
                {
                    ciItem.Vars.Add("INSTANCE", ciItem.Instance);
                }
                else
                {
                    ciItem.Vars["INSTANCE"] = ciItem.Instance;
                }

                // Oracle

            }
        }
    }
}
