using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class InstanceRulesProd : IInstanceRules
    {
        public void ApplyInstanceRules(HostOrService hs, IDictionary<Guid, Group> groups)
        {
            /*
            *********************************************************************
               ALERTING
            *********************************************************************
            */
            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase,
                "profile-tma-mid-docker",
                "profile-all-mid-sdwan-meraki",
                "profile-all-mid-sdwan-silverpeak",
                "profile-all-mid-sdwan-viptela"
            ))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "ON"));

            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase,
                "profile-default-ping-only-noalert"
            ))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF"));

            // disable ALERTS for non-active and non-infoalerting
            if (hs.Status != "ACTIVE" && hs.Status != "INFOALERTING")
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));
            // disable ALERTS for DEV/QM ARGUS systems
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;
            if ((hs.Environment == "DEV" || hs.Environment == "QM") && (hs.AppSupportGroup == argusGroupCIID || hs.OSSupportGroup == argusGroupCIID))
                hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF", 1));

            /*
            *********************************************************************
               NRPE alle Profile die kein NRPE benoetigen hier eintragen
            *********************************************************************
            */
            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase,
                "profile-ami-host-ipconnection-only",
                "profile-ami-host-hdw-sensor",
                "profile-ami-network-mpls-2",
                "profile-ami-network-mpls-4",
                "profile-ami-network-firewall-low-mem",
                "profile-ami-network-firewall-single-low-mem",
                "profile-ami-network-switch-low-mem",
                "profile-ami-mid-datadomain",
                "profile-ami-mid-quorum-avamar",
                "profile-ami-mid-quorum-datadomain",
                "profile-ami-mid-quorum-datadomain-virtuell",
                "profile-ami-mid-quorum-datadomain-ex-ddboost",
                "profile-ami-app-flowmon",
                "profile-ami-app-moxis-http",
                "profile-ami-network-switch-auto-stacked",
                "profile-ami-network-switch-single-power-stacked",
                "profile-ami-app-turbox",
                "profile-ami-esx-server-perf",
                "profile-ami-esx-vm-perf",
                "profile-ami-app-turbox-license",
                "profile-ami-app-amscom-http",
                "profile-ami-cust-app-promis-httpadaptor",
                "profile-ami-host-dns-osram",
                "profile-ami-network-vzen",
                "profile-ami-network-mpls-1",
                "profile-ami-app-hsm",
                "profile-ami-sto-isilon-base",
                "profile-ami-sto-isilon-perf",
                "profile-ami-sto-isilon-cluster",
                "profile-ami-sto-storagegrid",
                "profile-ami-network-mpls-3",
                "profile-ami-host-port-check-no-icmp",
                "profile-ami-app-http-certificate-expiration",
                "profile-ami-app-tcp-certificate-expiration",
                "profile-ami-appl-cryptospike",
                "profile-ami-app-lsf-stat",
                "profile-default-mid-oracle-asm",
                "profile-default-mid-oracle-rac-db",
                "profile-default-mid-oracle-rac-db-noinc",
                "profile-default-mid-oracle-single-instance-db-win",
                "profile-default-virt-esxi-host-ex-lom-checks",
                "profile-default-virt-esxi-host",
                "profile-default-virt-esxi-host-vcenter-v7",
                "profile-default-virt-vcloud-vcd",
                "profile-default-virt-vrops",
                "profile-default-virt-vcloud-um",
                "profile-default-virt-vcloud-nsxmanager",
                "profile-default-virt-dsi-fortigate",
                "profile-default-virt-dsi-fortimanger",
                "profile-default-virt-vcloud-f5",
                "profile-default-virt-vrli",
                "profile-default-virt-vcloud-vcd-conspxy",
                "profile-default-virt-vcloud-portal",
                "profile-default-virt-vcloud-vcd-admportal",
                "profile-default-virt-vcloud-baas-portal",
                "profile-default-virt-vcloud-baas-frontend",
                "profile-default-virt-vcloud-draas-portal",
                "profile-default-virt-vcloud-draas-repl-manager",
                "profile-default-virt-vcloud-draas-tunnel",
                "profile-default-virt-vcloud-draas-replicator",
                "profile-intern_shared-virt-esxi-host-vclassic",
                "profile-intern_shared-virt-esxi-host-vcloud",
                "profile-default-hw-mon-only",
                "profile-all-mid-sdwan-meraki",
                "profile-all-mid-sdwan-silverpeak",
                "profile-all-mid-sdwan-silverpeak-noticket",
                "profile-all-mid-sdwan-viptela",
                "profile-all-mid-sdwan-viptela-noticket",
                "profile-default-app-naemon-eventgenerator",
                "profile-default-mid-db-mssql",
                "profile-default-mid-db-mssql-express",
                "profile-default-mid-db-mssql-ex-dbjobs",
                "profile-default-mid-db-mssql-2008",
                "profile-default-mid-db-mssql-2017",
                "profile-uni-mid-db-mssql",
                "profile-default-mid-oracle-rac-in-dg",
                "profile-default-mid-oracle-dg-db",
                "profile-default-mid-oracle-dg-db-bkpi",
                "profile-default-mid-oracle-lsnrservice",
                "profile-default-ocum-server",
                "profile-default-ocum-api-server",
                "profile-default-virt-vcenter-embpsc",
                "profile-default-virt-vcenter-embpsc-ipv4",
                "profile-default-virt-vcenter-embpsc-vcenter-v7",
                "profile-default-snapcenter-server",
                "profile-default-vsc-server",
                "profile-ami-host-ping-only",
                "profile-default-ping-only",
                "profile-default-ping-only-noalert",
                "profile-default-mid-datadomain",
                "profile-default-hw-idpa-torswitch",
                "profile-default-mid-quorum-datadomain-idpa",
                "profile-default-mid-quorum-avamar_libpqxx5",
                "profile-default-mid-quorum-avamar-idpa_libpqxx5",
                "profile-default-mid-quorum-vsphere-idpa",
                "profile-ami-appl-bomgar",
                "profile-phg-app-adds-ssl",
                "profile-phg-app-prinzhorn-webserver",
                "profile-phg-virt-dsi-fortigate",
                "profile-phg-virt-dsi-fortimanger",
                "profile-phg-virt-psc-external",
                "profile-phg-virt-vcenter-embpsc",
                "profile-phg-virt-vcenter-extpsc",
                "profile-phg-virt-vcloud-baas-portal",
                "profile-phg-virt-vcloud-f5",
                "profile-phg-virt-vcloud-nsxmanager",
                "profile-phg-virt-vrops",
                "profile-phg-virt-esxi-host",
                "profile-uni-app-automic-lnx-agent-v12-2301",
                "profile-uni-app-pega-web-hotfix",
                "profile-uni-app-pega-web-int",
                "profile-uni-app-pega-web-monitor",
                "profile-uni-app-pega-web-preprod",
                "profile-uni-app-pega-web-prod",
                "profile-uni-app-pega-web-public",
                "profile-uni-app-pega-web-train",
                "profile-uni-app-pega-web-dev",
                "profile-uni-app-pega-web",
                "profile-uni-app-pega-jboss",
                "profile-uni-app-web-hotfix",
                "profile-uni-app-web-integration",
                "profile-uni-app-web-lasttest",
                "profile-uni-app-web-preprod",
                "profile-uni-app-web-prod",
                "profile-uni-app-web-entwicklung",
                "profile-uni-host-osbase-iseries",
                "profile-default-mid-avamar-health",
                "profile-default-mid-oracle-oem",
                "profile-default-app-checks-assigned-to-host",
                "profile-ami-virt-esxi-host",
                "profile-uni-app-sirius-ecm-filenet",
                "profile-ami-network-switch-3850",
                "profile-ami-network-testprofile",
                "profile-phg-app-witron-jobs",
                "profile-default-host-up-port-check",
                "profile-uni-app-sirius-partnersuche",
                "profile-uni-app-sirius-pegaplattform",
                "profile-uni-app-sirius-pegakubi",
                "profile-uni-app-automic-lnx-agent-v12",
                "profile-uni-app-splunkforwarder",
                "profile-default-ctx-adc",
                "profile-default-mid-mysql-min",
                "profile-default-mid-mysql",
                "profile-default-mid-mysql-slave",
                "profile-ami-network-switch-nexus",
                "profile-ami-network-switch-auto",
                "profile-ami-network-firewall-auto",
                "profile-ami-network-firewall-single-auto",
                "profile-ami-network-switch-single-power",
                "profile-ami-network-firewall-virtual",
                "profile-uni-app-ecm-brs-app-cpe-noalert",
                "profile-uni-app-ecm-brs-app-cpe-platin",
                "profile-uni-app-ecm-tika-app-cpe-noalert",
                "profile-uni-app-ecm-tika-app-cpe-platin",
                "profile-uni-app-automic-uan-connection"
            ))
                hs.AddVariable(new Variable("HASNRPE", "FIXED", "NO"));

            /*
            *********************************************************************
               disable Hardware Monitoring based by profile
            *********************************************************************
            */
            if (hs.HasAnyProfileOf(StringComparison.InvariantCultureIgnoreCase,
                "profile-default-virt-esxi-host",
                "profile-default-virt-esxi-host-vcenter-v7",
                "profile-intern_shared-virt-esxi-host-vclassic",
                "profile-intern_shared-virt-esxi-host-vcloud",
                "profile-phg-virt-esxi-host"
            ))
                hs.AddVariable(new Variable("DYNAMICMODULES_LOM", "FIXED", "OFF"));


            /*
            *********************************************************************
                dynamic capability
            *********************************************************************
            */
            if (hs.HasProfileMatchingRegex("^profiletsc-.*"))
            {
                hs.AddVariable(new Variable("HASNRPE", "FIXED", "NO"));
                hs.Tags.Add("cap_scope_tsc_yes");
            }
            else
                hs.Tags.Add("cap_scope_tsc_no");

            /*
            *********************************************************************
                customer scoping
            *********************************************************************
            */

            var capCust = $"cap_cust_{hs.CustomerNickname.ToLowerInvariant()}";
            hs.Tags.Add(capCust);

            // build negated sdwan capability
            if (!hs.Tags.Contains("cap_sdwan"))
                hs.Tags.Add("cap_nosdwan");

            // add customer specific scoping
            if (hs.CustomerNickname == "TMA")
            {
                hs.Tags.Add("cap_scope_tmamain");

                /* TMA dmz systems
                    IFVLAN:
                        vlan21
                        21
                        LAN21
                     TODO add vlan / interface filter to ensure future hosts are added dynamically
                 */
                var hostlist = new Dictionary<string, IEnumerable<string>>()
                {
                    {"TMA-dmz", new List<string>() { "H018430" } }
                };
                if (hostlist["TMA-dmz"].Contains(hs.ID))
                {
                    hs.Tags.Remove("cap_scope_tmamain");
                    hs.Tags.Add("cap_scope_tmadmz");

                }
            }
            else if (hs.CustomerNickname == "INTERN")
            {
                hs.Tags.Add("cap_scope_internmain");
                if (Regex.IsMatch(hs.Name ?? "", "svclxnaemp", RegexOptions.IgnoreCase))
                {
                    hs.Tags.Remove("cap_scope_internmain");
                    hs.Tags.Add("cap_scope_internmom");
                }
            }
            else if (hs.CustomerNickname == "AMSINT")
            {
                hs.Tags.Add("cap_scope_amsintmain");
                if (hs.Name != null && new string[] {
                    "nagiosup01",
                    "nagiosup02",
                    "nagiostp01",
                    "nagiostp02",
                    "nagiostx01",
                    "nagiostx02",
                    "NAGIOSUP01",
                    "NAGIOSUP02",
                    "NAGIOSTP01",
                    "NAGIOSTP02",
                    "NAGIOSTX01",
                    "NAGIOSTX02",
                    "METRICS-SENDER ON NAGIOSTP01",
                    "METRICS-SENDER ON NAGIOSTP02",
                    "METRICS-SENDER ON NAGIOSTX01",
                    "METRICS-SENDER ON NAGIOSTX02",
                    "METRICS-SENDER ON NAGIOSUP01",
                    "METRICS-SENDER ON NAGIOSUP02",
                    }.Contains(hs.Name))
                {
                    hs.Tags.Remove("cap_scope_amsintmain");
                    hs.Tags.Add("cap_scope_amsintmom");
                }
            }
            else if (hs.CustomerNickname == "BGT")
            {
                hs.Tags.Add("cap_scope_bgtmain");
                if (hs.Name != null && new string[] {
                    "pbsprd-mon-01",
                    "PBSPRD-MON-01",
                    "pbsparmon01",
                    "PBSPARMON01",
                    "METRICS-SENDER ON PBSPARMON01",
                    "METRICS-SENDER ON PBSPRD-MON-01"
                    }.Contains(hs.Name))
                {
                    hs.Tags.Remove("cap_scope_bgtmain");
                    hs.Tags.Add("cap_scope_bgtmom");
                }
            }
            else if (hs.CustomerNickname == "TS-CH")
            {
                hs.Tags.Add("cap_scope_ts-chmain");
                if (hs.Name != null && new string[] {
                    "uvairz5091",
                    "UVAIRZ5091",
                    "uvairz5112",
                    "UVAIRZ5112",
                    "uvairz5113",
                    "UVAIRZ5113",
                    "METRICS-SENDER ON UVAIRZ5091",
                    "METRICS-SENDER ON UVAIRZ5112",
                    "METRICS-SENDER ON UVAIRZ5113"
                    }.Contains(hs.Name))
                {
                    hs.Tags.Remove("cap_scope_ts-chmain");
                    hs.Tags.Add("cap_scope_ts-chmom");
                }
            }

            /*
            *********************************************************************
               capabilities by variables
            *********************************************************************
            */
            // TSMNetworkRequired
            if (hs.GetVariableValue("TSMNETWORKREQUIRED") == "YES")
                hs.Tags.Add("cap_tsm_monitoring");
            // DMZNETWORKREQUIRED
            if (hs.GetVariableValue("DMZNETWORKREQUIRED") == "YES")
            {
                hs.Tags.Add("cap_scope_dmz");
                hs.Tags.Remove("cap_nosdwan");
            } else
            {
                if (!hs.Tags.Contains("cap_sdwan"))
                    hs.Tags.Add("cap_scope_nodmz");
            }

            /*
            *********************************************************************
               capabilities for scoping UAN enforced CIs
            *********************************************************************
            */

            // DSSMITH
            if (!hs.Tags.Contains("cap_dssmith_uan") && hs.CustomerNickname.Equals("DSSMITH", StringComparison.InvariantCultureIgnoreCase))
                hs.Tags.Add("cap_dssmith_nonuan");

            // PRINZHORN
            if (!hs.Tags.Contains("cap_prinzhorn_uan") && hs.CustomerNickname.Equals("PRINZHORN", StringComparison.InvariantCultureIgnoreCase))
                hs.Tags.Add("cap_prinzhorn_nonuan");

            // UNIQA
            if (!hs.Tags.Contains("cap_uniqa_uan") && hs.CustomerNickname.Equals("UNIQA", StringComparison.InvariantCultureIgnoreCase))
                hs.Tags.Add("cap_uniqa_nonuan");
        }

        private static HashSet<string> RelevantStatuus = new HashSet<string>()
            {
                "ACTIVE",
                "INFOALERTING",
                "BASE_INSTALLED",
                "READY_FOR_SERVICE",
                "EXPERIMENTAL",
                "HOSTING",
            };
        public bool FilterTarget(HostOrService hs)
        {
            return RelevantStatuus.Contains(hs.Status ?? "");
        }

        public bool FilterCustomer(string customerNickname)
        {
            return ValidCustomers.Contains(customerNickname);
        }

        public bool FilterProfileFromCmdbCategory(Category category)
        {
            return category.Group == "MONITORING" &&
                (Regex.IsMatch(category.Name, "profile-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(category.Name, "profiletsc-.*", RegexOptions.IgnoreCase));
        }
        public bool FilterNaemonInstance(NaemonInstanceV1 naemonInstance)
        {
            return ValidNaemonInstances.Contains(naemonInstance.Name);
        }

        private static string[] ValidNaemonInstances = new string[]
        {
            "svphg200mon001",
            "svphg200mon002",
            "uansvclxnaemp01",
            "uansvclxnaemp02",
            "uansvclxnaemp03",
            "uansvclxnaemp04",
            "uansvclxnaemp05",
            "uansvclxnaemp06",
            "uansvclxnaemp07",
            "uansvclxnaemp08",
            "uansvclxnaemp09",
            "uansvclxnaemp10",
            "uansvclxnaemp91",
            "uansvclxnaemq01",
            "dmzlxnaemp01",
            "dmzlxnaemp02",
            "lxmonargusp01",
            "svuitpmon01",
            "svuitpmon03",
            "nagiosup01",
            "nagiosup02",
            "nagiostx01",
            "nagiostx02",
            "nagiostp01",
            "nagiostp02",
            "deznaemd01",
            "pbsaurmon01",
            "pbsparmon01",
            "pbsprd-mon-01",
            "dmztsilxnaemp01",
            "dmztsilxnaemp02",
            "svdsmpmon01",
            "svdsmpmon02",
            "uvairz5091",
            "uvairz5112",
            "uvairz5113",
        };

        private static string[] ValidCustomers = new string[]
        {
            "ADISSEO",
            "AGRANA",
            "AMS",
            "AMSINT",
            "ANDRITZ",
            "ATS",
            "AVESTRA",
            "AWS",
            "BESI",
            "BGT",
            "BUAK",
            "BUERKERT-SP-SDW",
            "BUOE",
            "CCENERGIE",
            "COVESTRO-SP-SDW",
            "DELFORTGROUP",
            "DELFORT-SP-SDW",
            "DOL-FRUTURA",
            "DSSMITH",
            "EDITEL",
            "EGSTON",
            "ELECTROTERMINAL",
            "ELIN",
            "EUH-SP-SDW",
            "FREQUENTIS",
            "GIS",
            "GREINER",
            "HAAS",
            "HERVIS",
            "INNO-LAB",
            "INSELSPITAL",
            "INTERN",
            "INTERN_SHARED",
            "INTERN_SI",
            "IPSOFT",
            "ISOVOLTA",
            "ITG",
            "JUNGBUNZLAUER",
            "KFA",
            "KHELISABETHINEN",
            "KHSCHWARZACH",
            "KHSTP",
            "KOMMUNALKREDIT",
            "KRAGES",
            "LANDGARD-SP-SDW",
            "MAGNA-AUTOMOTIVE",
            "MAGNA-TRANSMISSION-SYSTEMS",
            "MAN",
            "MEDMOBILE",
            "MONOPOLVERWALTUNG",
            "PIERRELANG",
            "PMG",
            "PMG-SP-SDW",
            "PMT-SP-SDW",
            "POLYTEC",
            "PRIMETALS",
            "PRINZHORN",
            "PZH-SP-SDW",
            "ROHOEL",
            "SALESIANER",
            "SAMSUNG",
            "SAPPI",
            "SBB",
            "SEMPERIT",
            "SI AUDIMATCH",
            "SWARCO",
            "SWISSCARD",
            "TIGER",
            "TIGER-SP-SDW",
            "TMA",
            "TROGROUP",
            "TS SCHWEIZ",
            "TSA",
            "TSA-SPTEST-SDW",
            "TSA-SP-SDW",
            "TS-CH",
            "UNIPER-IT-SP-SDW",
            "UNIQA",
            "VALORA",
            "VOEST",
            "VOESTALPIN-SDW",
            "VOESTALPIN-SP-SDW",
            "VZUG",
            "WIENERBERGER",
            "WIRTSCHAFTSVERLAG",
            "WRWHAB",
            "WRWKS",
            "VW-SP-SDW",
            "INFOSYS-DAG-SP-SDW",
            "ZOLLNER-SP-SDW",
            "BESI-SP-SDW",
            "DTGBS-TEST-VN-SDW",
            "MSIG-SP-SDW",
            "UMDASCH-SP-SDW"
        };
    }
}
