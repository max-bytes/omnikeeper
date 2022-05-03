using System.Collections.Generic;
using System.Text.RegularExpressions;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void UpdateProfileField(List<ConfigurationItem> ciData, List<string> cmdbMonprofilePrefix)
        {
            foreach (var ciItem in ciData)
            {
                var profileCount = 0;
                ciItem.Profile = "NONE";
                ciItem.ProfileOrg = new List<string>();

                if (ciItem.Categories.ContainsKey("MONITORING"))
                {
                    foreach (var category in ciItem.Categories["MONITORING"])
                    {
                        // check profile against configured scoping pattern
                        var isMyProfileScope = false;
                        foreach (var pattern in cmdbMonprofilePrefix)
                        {
                            if (Regex.IsMatch(category.Name, $"^{pattern}", RegexOptions.IgnoreCase))
                            {
                                isMyProfileScope = true;
                                break;
                            }
                        }

                        if (isMyProfileScope)
                        {
                            profileCount += 1;
                            ciItem.Profile = category.Name.ToLower();
                            ciItem.ProfileOrg.Add(category.Name.ToLower());
                        }
                    }
                }

                if (profileCount > 1)
                {
                    ciItem.Profile = "MULTIPLE";
                }

                if (profileCount == 1 && Regex.IsMatch(ciItem.Profile, "^profile", RegexOptions.IgnoreCase))
                {
                    // add legacy profile capability
                    ciItem.Tags.Add("cap_lp_" + ciItem.Profile);
                }

            }

        }
    
        public static void UpdateProfileField(Dictionary<string, ConfigurationItem> ciData, List<string> cmdbMonprofilePrefix) { }
    }
}
