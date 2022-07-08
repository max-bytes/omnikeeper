using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class InstanceRulesDev : IInstanceRules
    {
        public void ApplyInstanceRules(HostOrService hs, IDictionary<Guid, Group> groups)
        {
            /*
            *********************************************************************
               ALERTING
            *********************************************************************
            */
            hs.AddVariable(new Variable("ALERTS", "FIXED", "ON"));

            // disable ALERTS for non-active and non-infoalerting
            if (hs.Status != "ACTIVE" && hs.Status != "INFOALERTING")
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));
            // disable ALERTS for DEV/QM ARGUS systems
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;
            if ((hs.Environment == "DEV" || hs.Environment == "QM") && (hs.AppSupportGroup == argusGroupCIID || hs.OSSupportGroup == argusGroupCIID))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));

            // enable tsi sdwan silverpeak test
            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-silverpeak-device", "profiledynamic-tsi-silverpeak-orchestrator"))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "ON", 2));

            // enable tsi sdwan versa test
            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-versa-device", "profiledynamic-tsi-versa-orchestrator"))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "ON", 2));

            /*
            *********************************************************************
                customer scoping
            *********************************************************************
            */
            var capCust = $"cap_cust_{hs.CustomerNickname.ToLowerInvariant()}";
            hs.Tags.Add(capCust);
        }

        public bool FilterTarget(HostOrService hs)
        {
            return true;
        }

        public bool FilterCustomer(string customerNickname)
        {
            return ValidCustomers.Contains(customerNickname);
        }

        public bool FilterProfileFromCmdbCategory(Category category)
        {
            return category.Group == "MONITORING" &&
                (Regex.IsMatch(category.Name, "profile-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(category.Name, "profiletsc-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(category.Name, "profiledev-.*", RegexOptions.IgnoreCase));
        }

        public bool FilterNaemonInstance(NaemonInstanceV1 naemonInstance)
        {
            return ValidNaemonInstances.Contains(naemonInstance.Name);
        }

        private static string[] ValidNaemonInstances = new string[]
        {
            "uansvclxnaemd01",
             "uansvclxnaemd02",
             "svuitemon01",
             "deznaemd01",
             "dmztsilxnaemd01"
        };

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
