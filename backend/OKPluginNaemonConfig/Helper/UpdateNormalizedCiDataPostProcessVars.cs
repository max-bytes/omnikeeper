using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static OKPluginNaemonConfig.NaemonConfig;

namespace OKPluginNaemonConfig.Helper
{
    public static partial class CIData
    {
        public static void UpdateNormalizedCiDataPostProcessVars(List<ConfigurationItem> ciData)
        {
            foreach (var ciItem in ciData)
            {
                /* ensure dynamic module injection off for ping-only patterns */

                // NOTE original php pattern '/ping-only/i'

                if (Regex.IsMatch(ciItem.Profile, "ping-only", RegexOptions.IgnoreCase))
                {
                    if (!ciItem.Vars.ContainsKey("DYNAMICMODULES"))
                    {
                        ciItem.Vars.Add("DYNAMICMODULES", "NO");
                    }
                    else
                    {
                        ciItem.Vars["DYNAMICMODULES"] = "NO";
                    }
                }

                /* dynamic profiling here if no profile is set and CI enabled for dynamicadd
                 *
                * detect agent on OS in a simple manner
                */

                if (
                    (ciItem.Profile == "NONE" || ciItem.Profile == "MULTIPLE") && ciItem.Type == "HOST" &&
                    (
                        Regex.IsMatch(ciItem.CmdbData["HOS"], "/^WIN/") ||
                        Regex.IsMatch(ciItem.CmdbData["HOS"], "/^LINUX/") ||
                        Regex.IsMatch(ciItem.CmdbData["HOS"], "/^XEN/") ||
                        Regex.IsMatch(ciItem.CmdbData["HOS"], "/^SUNOS/")
                        ) &&
                    (
                     Regex.IsMatch(ciItem.CmdbData["HPLATFORM"], "/^SRV_/")
                    ) &&
                    (
                        Regex.IsMatch(ciItem.CmdbData["HSTATUS"], "/ACTIVE/") ||
                        Regex.IsMatch(ciItem.CmdbData["HSTATUS"], "/INFOALERTING/") ||
                        Regex.IsMatch(ciItem.CmdbData["HSTATUS"], "/BASE_INSTALLED/") ||
                        Regex.IsMatch(ciItem.CmdbData["HSTATUS"], "/READY_FOR_SERVICE/")
                    ) &&
                    ciItem.Vars["DYNAMICADD"] == "YES"
                    )
                {
                    ciItem.Profile = "dynamic-nrpe";
                }


                /* update CI metavars to vars field and override meta fields */

                if (!ciItem.Vars.ContainsKey("CIID"))
                {
                    ciItem.Vars.Add("CIID", ciItem.Id);
                } else
                {
                    ciItem.Vars["CIID"] = ciItem.Id;
                }

                if (!ciItem.Vars.ContainsKey("CINAME"))
                {
                    ciItem.Vars.Add("CINAME", ciItem.Name);
                }
                else
                {
                    ciItem.Vars["CINAME"] = ciItem.Name;
                }

                if (!ciItem.Vars.ContainsKey("CONFIGSOURCE"))
                {
                    ciItem.Vars.Add("CONFIGSOURCE", "monmanagement");
                }
                else
                {
                    ciItem.Vars["CONFIGSOURCE"] = "monmanagement";
                }

                if (!ciItem.Vars.ContainsKey("MONITORINGPROFILE"))
                {
                    ciItem.Vars.Add("MONITORINGPROFILE", ciItem.Profile);
                }
                else
                {
                    ciItem.Vars["MONITORINGPROFILE"] = ciItem.Profile;
                }

                if (!ciItem.Vars.ContainsKey("MONITORINGPROFILE_ORIG"))
                {
                    ciItem.Vars.Add("MONITORINGPROFILE_ORIG", string.Join(",", ciItem.ProfileOrg));
                }
                else
                {
                    ciItem.Vars["MONITORINGPROFILE_ORIG"] = string.Join(",", ciItem.ProfileOrg);
                }

                if (!ciItem.Vars.ContainsKey("CUST"))
                {
                    ciItem.Vars.Add("CUST", ciItem.Cust);
                }
                else
                {
                    ciItem.Vars["CUST"] = ciItem.Cust;
                }

                if (!ciItem.Vars.ContainsKey("CUST_ESCAPED"))
                {
                    ciItem.Vars.Add("CUST_ESCAPED", EscapeCustomerCode(ciItem.Cust));
                }
                else
                {
                    ciItem.Vars["CUST_ESCAPED"] = EscapeCustomerCode(ciItem.Cust);
                }

                if (!ciItem.Vars.ContainsKey("ENVIRONMENT"))
                {
                    ciItem.Vars.Add("ENVIRONMENT", ciItem.Environment);
                }
                else
                {
                    ciItem.Vars["ENVIRONMENT"] = ciItem.Environment;
                }

                if (!ciItem.Vars.ContainsKey("STATUS"))
                {
                    ciItem.Vars.Add("STATUS", ciItem.Status);
                }
                else
                {
                    ciItem.Vars["STATUS"] = ciItem.Status;
                }

                if (!ciItem.Vars.ContainsKey("STATUS"))
                {
                    ciItem.Vars.Add("STATUS", ciItem.Status);
                }
                else
                {
                    ciItem.Vars["STATUS"] = ciItem.Status;
                }

                if (!ciItem.Vars.ContainsKey("CRITICALITY"))
                {
                    ciItem.Vars.Add("CRITICALITY", ciItem.Criticality);
                }
                else
                {
                    ciItem.Vars["CRITICALITY"] = ciItem.Criticality;
                }

                if (!ciItem.Vars.ContainsKey("SUPP_OS"))
                {
                    ciItem.Vars.Add("SUPP_OS", ciItem.SuppOS);
                }
                else
                {
                    ciItem.Vars["SUPP_OS"] = ciItem.SuppOS;
                }

                if (!ciItem.Vars.ContainsKey("SUPP_APP"))
                {
                    ciItem.Vars.Add("SUPP_APP", ciItem.SuppApp);
                }
                else
                {
                    ciItem.Vars["SUPP_APP"] = ciItem.SuppApp;
                }
            }
        }


        private static string EscapeCustomerCode(string customer)
        {
            customer = customer.Replace(" ", "-");
            customer = customer.Replace("(", "-");
            customer = customer.Replace(")", "-");
            customer = customer.Replace("[", "-");
            customer = customer.Replace("]", "-");
            return customer;
        }
    }
}
