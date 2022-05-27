using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class InstanceRulesDev : IInstanceRules
    {
        public void ApplyInstanceRules(HostOrService hs, IDictionary<Guid, Group> groups)
        {
            hs.AddVariable(new Variable("ALERTS", "FIXED", "ON"));

            // disable ALERTS for non-active and non-infoalerting
            if (hs.Status != "ACTIVE" && hs.Status != "INFOALERTING")
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));
            // disable ALERTS for DEV/QM ARGUS systems
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;
            if ((hs.Environment == "DEV" || hs.Environment == "QM") && (hs.AppSupportGroup == argusGroupCIID || hs.OSSupportGroup == argusGroupCIID))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));

            //        if (in_array($ci['PROFILE'], ['profiledynamic-tsi-silverpeak-device', 'profiledynamic-tsi-silverpeak-orchestrator']))
            //        {
            //$resultRef[$id]['VARS']['ALERTS'] = 'ON';
            //        }

            //        /* enable tsi sdwan versa test */
            //        if (in_array($ci['PROFILE'], ['profiledynamic-tsi-versa-device', 'profiledynamic-tsi-versa-orchestrator']))
            //        {
            //$resultRef[$id]['VARS']['ALERTS'] = 'ON';
            //        }

            // TODO

            var capCust = $"cap_cust_{hs.Customer.Nickname.ToLowerInvariant()}";
            hs.Tags.Add(capCust);

            // TODO



            if (hs.HasProfile("profiledev-default-app-naemon-eventgenerator"))
            {
                hs.Tags.Clear();
                hs.Tags.Add("cap_eventgenerator");
            }

        }

        public bool FilterTarget(HostOrService hs)
        {
            return true;
        }

        public bool FilterCustomer(Customer customer)
        {
            return ValidCustomers.Contains(customer.Nickname);
        }

        public bool FilterProfileFromCmdbCategory(Category category)
        {
            return category.Group == "MONITORING" &&
                (Regex.IsMatch(category.Name, "profile-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(category.Name, "profiletsc-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(category.Name, "profiledev-.*", RegexOptions.IgnoreCase));
        }

        public bool FilterNaemonInstance(NaemonInstanceV1 naemonInstance)
        {
            return true;
        }

        private static string[] ValidCustomers = new string[]
        {
            "ADISSEO",
            "AMSINT",
            "ATS",
            "AVESTRA",
            "AWS",
            "BGT",
            "BUAK",
            "DELFORTGROUP",
            "DSSMITH",
            "EDITEL",
            "ELIN",
            "HERVIS",
            "INNO-LAB",
            "INTERN",
            "INTERN_SHARED",
            "ISOVOLTA",
            "ITG",
            "JUNGBUNZLAUER",
            "KHSCHWARZACH",
            "KHSTP",
            "KOMMUNALKREDIT",
            "MAGNA-AUTOMOTIVE",
            "MAN",
            "MEDMOBILE",
            "PMG",
            "POLYTEC",
            "PRINZHORN",
            "PRIMETALS",
            "SALESIANER",
            "SEMPERIT",
            "TIGER",
            "TMA",
            "TSA",
            "TSA-SPTEST-SDW",
            "COVESTRO-SP-SDW",
            "SAPPI",
            "UNIQA",
            "VOEST",
            "VOESTALPIN-SP-SDW",
            "TS SCHWEIZ",
            "LANDGARD-SP-SDW",
            "UNIPER-SP-SDW",
            "BUERKERT-SP-SDW",
            "EUH-SP-SDW",
            "PMG-SP-SDW",
            "TIGER-SP-SDW",
            "INFOSYS-DAG-SP-SDW",
            "ZOLLNER-SP-SDW",
            "BESI-SP-SDW",
            "DTGBS-TEST-VN-SDW",
            "VW-SP-SDW",
            "KHELISABETHINEN",
            "MAGNA-TRANSMISSION-SYSTEMS",
        };
    }
}
