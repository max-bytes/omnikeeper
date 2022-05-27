using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class CLBNaemonVariableResolution : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ILayerModel layerModel;
        private readonly IAttributeModel attributeModel;
        public CLBNaemonVariableResolution(ICIModel ciModel, ILayerModel layerModel, IEffectiveTraitModel effectiveTraitModel,
            IRelationModel relationModel, IAttributeModel attributeModel)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.layerModel = layerModel;
            this.attributeModel = attributeModel;
        }

        public enum Stage
        {
            DEV, PROD
        }

        private Configuration ParseConfig(JsonDocument configJson)
        {
            var tmpCfg = JsonSerializer.Deserialize<Configuration>(configJson);
            if (tmpCfg == null)
                throw new Exception("Could not parse configuration");
            return tmpCfg;
        }

        public override ISet<string>? GetDependentLayerIDs(JsonDocument config, ILogger logger)
        {
            try
            {
                var cfg = ParseConfig(config);
                return cfg.CMDBInputLayerSet
                    .Union(cfg.MonmanV1InputLayerSet)
                    .Union(cfg.SelfserviceVariablesInputLayerSet)
                    .ToHashSet();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot get dependent layers");
                return null;
            }
        }

        public override async Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            Configuration cfg = ParseConfig(config);

            var stage = Stage.DEV; // TODO: make configurable

            var timeThreshold = changesetProxy.TimeThreshold;

            var cmdbInputLayerset = await layerModel.BuildLayerSet(cfg.CMDBInputLayerSet.ToArray(), trans);
            var monmanV1InputLayerset = await layerModel.BuildLayerSet(cfg.MonmanV1InputLayerSet.ToArray(), trans);
            var selfserviceVariablesInputLayerset = await layerModel.BuildLayerSet(cfg.SelfserviceVariablesInputLayerSet.ToArray(), trans);

            var targetHostModel = new GenericTraitEntityModel<TargetHost, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var targetServiceModel = new GenericTraitEntityModel<TargetService, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var naemonV1VariableModel = new GenericTraitEntityModel<NaemonVariableV1, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var selfServiceVariableModel = new GenericTraitEntityModel<SelfServiceVariable, (string refType, string refID, string name)>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var customerModel = new GenericTraitEntityModel<Customer, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var profileModel = new GenericTraitEntityModel<Profile, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var categoryModel = new GenericTraitEntityModel<Category, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var serviceActionModel = new GenericTraitEntityModel<ServiceAction, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var interfaceModel = new GenericTraitEntityModel<Interface, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var groupModel = new GenericTraitEntityModel<Group, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var naemonInstanceModel = new GenericTraitEntityModel<NaemonInstanceV1, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var tagModel = new GenericTraitEntityModel<TagV1>(effectiveTraitModel, ciModel, attributeModel, relationModel);

            var categories = await categoryModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);

            var cmdbProfiles = categories
                .Where(kv => kv.Value.Group == "MONITORING")
                .Where(kv =>
                {
                    var name = kv.Value.Name;
                    return stage switch
                    {
                        Stage.DEV => Regex.IsMatch(name, "profile-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(name, "profiletsc-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(name, "profiledev-.*", RegexOptions.IgnoreCase),
                        Stage.PROD => Regex.IsMatch(name, "profile-.*", RegexOptions.IgnoreCase) || Regex.IsMatch(name, "profiletsc-.*", RegexOptions.IgnoreCase),
                        _ => false,
                    };
                })
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var hosts = await targetHostModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var services = await targetServiceModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var customers = await customerModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var naemonV1Variables = await naemonV1VariableModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);
            var selfServiceVariables = await selfServiceVariableModel.GetAllByCIID(selfserviceVariablesInputLayerset, trans, timeThreshold);
            var profiles = await profileModel.GetAllByDataID(monmanV1InputLayerset, trans, timeThreshold);
            var serviceActions = await serviceActionModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var interfaces = await interfaceModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var groups = await groupModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var naemonInstances = await naemonInstanceModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);
            var tags = await tagModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);

            // construct an intermediate data structure holding hosts and services together
            List<string> CalculateCIProfiles(Guid[] memberOfCategories) => 
                memberOfCategories
                .Where(categoryCIID => cmdbProfiles.ContainsKey(categoryCIID))
                .Select(p => cmdbProfiles[p].Name)
                .ToList();
            Customer? CalculateCustomer(Guid? customerCIID) => 
                (customerCIID.HasValue && customers.TryGetValue(customerCIID.Value, out var customer)) ? customer : null;

            var hos = new Dictionary<Guid, HostOrService>(hosts.Count + services.Count);
            foreach (var kv in hosts)
            {
                var profilesOfCI = CalculateCIProfiles(kv.Value.MemberOfCategories);
                var customer = CalculateCustomer(kv.Value.Customer);
                if (customer == null)
                {
                    logger.LogWarning($"Could not lookup customer of host with CI-ID \"{kv.Key}\"... skipping");
                    continue;
                }
                hos.Add(kv.Key, new HostOrService(kv.Value, null, profilesOfCI, customer));
            }
            foreach (var kv in services)
            {
                var profilesOfCI = CalculateCIProfiles(kv.Value.MemberOfCategories);
                var customer = CalculateCustomer(kv.Value.Customer);
                if (customer == null)
                {
                    logger.LogWarning($"Could not lookup customer of service with CI-ID \"{kv.Key}\"... skipping");
                    continue;
                }
                hos.Add(kv.Key, new HostOrService(null, kv.Value, profilesOfCI, customer));
            }

            // filter hosts and services
            var relevantStatuus = new HashSet<string>()
            {
                "ACTIVE",
                "INFOALERTING",
                "BASE_INSTALLED",
                "READY_FOR_SERVICE",
                "EXPERIMENTAL",
                "HOSTING",
            };
            var filteredHOS = hos
                .Where(kv => relevantStatuus.Contains(kv.Value.Status ?? ""))
                //.Select(kv => (ciid: kv.Key, hs: kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // collect variables...

            // ...init
            // reference: updateNormalizedCiData_preProcessVars()
            foreach (var (ciid, hs) in filteredHOS)
            {
                hs.AddVariables(new List<Variable>()
                {
                    new Variable("ALERTS", "INIT", "OFF", -100),
                    new Variable("ALERTCIID", "INIT", hs.ID, -100),
                    new Variable("HASNRPE", "INIT", "YES", -100),
                    new Variable("DYNAMICADD", "INIT", "NO", -100),
                    new Variable("DYNAMICMODULES", "INIT", "YES", -100),
                });
            }

            // ...from variables in monman
            // reference: roughly updateNormalizedCiData_varsFromDatabase()
            var hosByNameLookup = filteredHOS.ToDictionary(h => h.Value.ID, h => h.Value);
            var customerNicknameLookup = customers.ToDictionary(c => c.Value.Nickname, c => c.Value);
            // TODO: this is not properly defined, because there can be multiple categories with the same name in different trees 
            // TODO: lookup how monman_v1 implements this and how it looks up the proper category from the profile
            // for now, we just pick the first that fits at random
            // also, we uppercase the category names
            var categoryNameLookup = categories.GroupBy(c => c.Value.Name).ToDictionary(t => t.Key.ToUpperInvariant(), t => t.First().Value);
            foreach (var v in naemonV1Variables)
            {
                // replace secret values with fetch command
                if (v.Value.isSecret)
                {
                    v.Value.value = $"$(python /opt2/nm-agent/bin/getSecret.py {v.Value.ID})";
                }

                var refID = v.Value.refID;
                switch (v.Value.refType)
                {
                    case "CI":
                        if (hosByNameLookup.TryGetValue(refID, out var foundHS))
                            foundHS.AddVariable(v.Value.ToResolvedVariable());
                        else
                            logger.LogWarning($"Could not find referenced CI with refID \"{refID}\" for variable \"{v.Value.ID}\", skipping variable");
                        break;
                    case "GLOBAL":
                        foreach (var rv in filteredHOS.Values)
                            rv.AddVariable(v.Value.ToResolvedVariable());
                        break;
                    case "PROFILE":
                        // approach: get the profile, look up its name, then fetch the corresponding CMDB category, then its member CIs
                        if (!long.TryParse(refID, out var refIDProfile))
                        {
                            logger.LogWarning($"Could not parse refID \"{refID}\" into number to look up profile");
                            break;
                        }
                        if (profiles.TryGetValue(refIDProfile, out var foundProfile))
                        {
                            var profileName = foundProfile.Name.ToUpperInvariant(); // transform to uppercase for proper comparison
                            if (categoryNameLookup.TryGetValue(profileName, out var category))
                            {
                                foreach (var targetCIID in category.Members)
                                {
                                    if (filteredHOS.TryGetValue(targetCIID, out var hs))
                                        hs.AddVariable(v.Value.ToResolvedVariable());
                                    else { } // member CI of category is neither host nor service, ignore
                                }
                            }
                            else
                            {
                                logger.LogWarning($"Could not find category with name \"{profileName}\", skipping variable");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"Could not find referenced profile with refID \"{refIDProfile}\" for variable \"{v.Value.ID}\", skipping variable");
                        }
                        break;
                    case "CUST":
                        if (customerNicknameLookup.TryGetValue(refID, out var foundCustomer))
                        {
                            foreach (var targetCIID in foundCustomer.AssociatedCIs)
                            {
                                if (filteredHOS.TryGetValue(targetCIID, out var hs))
                                    hs.AddVariable(v.Value.ToResolvedVariable());
                                else { } // associated CI of customer is neither host nor service, ignore
                            }
                        }
                        else
                        {
                            logger.LogWarning($"Could not find referenced customer with refID \"{refID}\" for variable \"{v.Value.ID}\", skipping variable");
                        }
                        break;
                    default:
                        logger.LogWarning($"Could not process monman variable \"{v.Value.ID}\": invalid refType \"{v.Value.refType}\"");
                        break;
                }
            }

            // ...from variables in self service
            foreach (var v in selfServiceVariables)
            {
                var refID = v.Value.refID;
                switch (v.Value.refType)
                {
                    case "CI":
                        if (hosByNameLookup.TryGetValue(refID, out var hs))
                            hs.AddVariable(v.Value.ToResolvedVariable());
                        else
                            logger.LogWarning($"Could not find referenced CI with refID \"{refID}\" for variable with ciid \"{v.Key}\", skipping variable");
                        break;
                    default:
                        logger.LogWarning($"Could not process selfservice variable with ciid \"{v.Key}\": invalid refType \"{v.Value.refType}\"");
                        break;
                }
            }

            string escapeCustomerNickname(string customerNickname)
            {
                return customerNickname
                    .Replace(' ', '-')
                    .Replace('(', '-')
                    .Replace(')', '-')
                    .Replace('[', '-')
                    .Replace(']', '-');
            }

            // ...from cmdb hosts and services
            // reference: roughly updateNormalizedCiData_varsByExpression(), naemon-vars-ci.php
            var serviceActionServiceIDLookup = serviceActions.Values.ToLookup(s => s.ServiceID);
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;
            foreach (var (ciid, hs) in filteredHOS)
            {
                hs.AddVariables(new List<Variable>()
                {
                    new Variable("LOCATION", "FIXED", Regex.Replace(hs.Location ?? "", @"\p{C}+", string.Empty)),
                    new Variable("OS", "FIXED", hs.OS ?? ""),
                    new Variable("PLATFORM", "FIXED", hs.Platform ?? ""),
                    new Variable("ADDRESS", "FIXED", hs.MonIPAddress ?? ""),
                    new Variable("PORT", "FIXED", hs.MonIPPort ?? ""),
                });

                if (stage == Stage.PROD)
                {
                    if (hs.Profiles.Any(p => Regex.IsMatch(p, "^profiletsc-.*")))
                        hs.AddVariable(new Variable("HASNRPE", "FIXED", "NO", 0));
                }

                // set alerting ID to foreign key for special instances
                if (hs.Instance == "SERVER-CH")
                    hs.AddVariable(new Variable("ALERTCIID", "FIXED", hs.ForeignKey ?? ""));

                // alerts
                if (hs.Status != "ACTIVE" && hs.Status != "INFOALERTING")
                    hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF")); // disable ALERTS for non-active and non-infoalerting
                else if ((hs.Environment == "DEV" || hs.Environment == "QM") && (hs.AppSupportGroup == argusGroupCIID || hs.OSSupportGroup == argusGroupCIID))
                    hs.AddVariable(new Variable("ALERTS", "FIXED", "OFF")); // disable ALERTS for DEV/QM ARGUS systems

                // host-specific stuff
                if (hs.Host != null)
                {
                    // hardware monitoring
                    foreach (var interfaceCIID in hs.Host.Interfaces)
                    {
                        if (interfaces.TryGetValue(interfaceCIID, out var @interface))
                        {
                            if (@interface.LanType?.Equals("MANAGEMENT", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                var interfaceName = @interface.Name;
                                if (interfaceName != null && (
                                    Regex.IsMatch(interfaceName, "ILO", RegexOptions.IgnoreCase) ||
                                    Regex.IsMatch(interfaceName, "XSCF", RegexOptions.IgnoreCase) ||
                                    Regex.IsMatch(interfaceName, "IDRAC", RegexOptions.IgnoreCase)
                                ))
                                {
                                    hs.AddVariables(new List<Variable>()
                                    {
                                        new Variable("LOMADDRESS", "FIXED", @interface.IP ?? ""),
                                        new Variable("LOMTYPE", "FIXED", @interface.Name?.ToUpperInvariant() ?? ""),
                                        new Variable("LOMNAME", "FIXED", @interface.DnsName ?? ""),
                                    });
                                    break;
                                }
                            }
                        }
                    }
                }

                // service-specific stuff
                if (hs.Service != null)
                {
                    // extract oracle db connection string
                    if (Regex.IsMatch(hs.Service.Class ?? "", "DB") && Regex.IsMatch(hs.Service.Type ?? "", "ORACLE"))
                    {
                        var foundServiceAction = serviceActionServiceIDLookup[hs.Service.ID].FirstOrDefault();
                        if (foundServiceAction != null)
                        {
                            if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                                hs.AddVariables(new List<Variable>()
                                {
                                    new Variable("ORACLECONNECT", "FIXED", (foundServiceAction.Command ?? "").Replace(" ", "")),
                                    new Variable("ORACLEUSER", "FIXED", foundServiceAction.CommandUser ?? ""),
                                });
                        }
                    }

                    // SD-WAN
                    if ((hs.Service.Class == "APP_ROUTING" || hs.Service.Class == "SVC_ROUTING") && Regex.IsMatch(hs.Service.Type ?? "", "^SD-WAN.*"))
                    {
                        var foundServiceAction = serviceActionServiceIDLookup[hs.Service.ID].FirstOrDefault();
                        if (foundServiceAction != null)
                        {
                            if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                                hs.AddVariables(new List<Variable>()
                                {
                                    new Variable("SDWANCHECKCONFIG", "FIXED", foundServiceAction.Command ?? ""),
                                    new Variable("SDWANORG", "FIXED", foundServiceAction.CommandUser ?? ""),
                                });
                        }
                    }
                }
            }

            // reference: updateNormalizedCiData_postProcessVars()
            foreach (var (ciid, hs) in filteredHOS)
            {
                // dynamically set profile under certain circumstances
                if (hs.Profiles.Count != 1 && hs.Host != null)
                {
                    var os = hs.Host.OS;
                    var osMatches = os != null && (
                        Regex.IsMatch(os, "^WIN") ||
                        Regex.IsMatch(os, "^LINUX") ||
                        Regex.IsMatch(os, "^XEN") ||
                        Regex.IsMatch(os, "^SUNOS")
                        );

                    var platformMatches = hs.Host.Platform != null && Regex.IsMatch(hs.Host.Platform, "^SRV_");

                    var status = hs.Host.Status;
                    var statusMatches = status != null && (
                        Regex.IsMatch(status, "ACTIVE") ||
                        Regex.IsMatch(status, "INFOALERTING") ||
                        Regex.IsMatch(status, "BASE_INSTALLED") ||
                        Regex.IsMatch(status, "READY_FOR_SERVICE")
                        );

                    if (osMatches && platformMatches && statusMatches)
                    {
                        if (hs.GetVariableValue("DYNAMICADD") == "YES")
                            hs.Profiles = new List<string>() { "dynamic-nrpe" };
                    }
                }

                var customerNickname = hs.Customer.Nickname;

                var osSupportGroupName = "UNKNOWN";
                if (hs.OSSupportGroup.HasValue && groups.TryGetValue(hs.OSSupportGroup.Value, out var osSupportGroup))
                    osSupportGroupName = osSupportGroup.Name;
                var appSupportGroupName = "UNKNOWN";
                if (hs.AppSupportGroup.HasValue && groups.TryGetValue(hs.AppSupportGroup.Value, out var appSupportGroup))
                    appSupportGroupName = appSupportGroup.Name;

                string monitoringProfile;
                if (hs.Profiles.Count == 1)
                    monitoringProfile = hs.Profiles[0];
                else if (hs.Profiles.Count > 1)
                    monitoringProfile = "MULTIPLE";
                else
                    monitoringProfile = "NONE";
                string monitoringProfileOrig;
                if (hs.Profiles.Count == 1)
                    monitoringProfileOrig = hs.Profiles[0];
                else if (hs.Profiles.Count > 1)
                    monitoringProfileOrig = string.Join(',', hs.Profiles);
                else
                    monitoringProfileOrig = "NONE";

                hs.AddVariables(new List<Variable>()
                {
                    new Variable("CIID", "FIXED", hs.ID),
                    new Variable("CINAME", "FIXED", hs.Name ?? ""),
                    new Variable("CONFIGSOURCE", "FIXED", "monmanagement"),

                    new Variable("MONITORINGPROFILE", "FIXED", monitoringProfile),
                    new Variable("MONITORINGPROFILE_ORIG", "FIXED", monitoringProfileOrig),

                    new Variable("CUST", "FIXED", customerNickname),
                    new Variable("CUST_ESCAPED", "FIXED", escapeCustomerNickname(customerNickname)),

                    new Variable("ENVIRONMENT", "FIXED", hs.Environment ?? ""),
                    new Variable("STATUS", "FIXED", hs.Status ?? ""),
                    new Variable("CRITICALITY", "FIXED", hs.Criticality ?? ""),

                    new Variable("SUPP_OS", "FIXED", osSupportGroupName),
                    new Variable("SUPP_APP", "FIXED", appSupportGroupName),

                    new Variable("INSTANCE", "FIXED", hs.Instance ?? ""),
                    new Variable("FSOURCE", "FIXED", hs.ForeignSource ?? ""),
                    new Variable("FKEY", "FIXED", hs.ForeignKey ?? ""),
                });
            }

            var debugOutput = false; // TODO: remove or make configurable

            // filter out hosts and services that contain no monitoring profile, because there is no use in creating resolved variables for them
            var evenMoreFilteredHOS = filteredHOS
                //.Where(kv => kv.Value.Variables.Any(v => v.Name == "MONITORINGPROFILE" && v.Value != "NONE"))
                .Where(kv => kv.Value.Profiles.Count != 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);


            // build capability map 
            // reference: getCapabilityMap()
            var capMap = new Dictionary<string, ISet<Guid>>(); // key: capability name, value: list of CIIDs of naemon instances
            foreach (var kv in naemonInstances)
            {
                var naemonCIID = kv.Key;
                var naemon = kv.Value;
                foreach (var tagCIID in naemon.Tags)
                {
                    if (tags.TryGetValue(tagCIID, out var tag))
                    {
                        if (Regex.IsMatch(tag.Name, "^cap_"))
                            capMap.AddNaemon(tag.Name, naemonCIID);
                    }
                    else
                    {
                        logger.LogWarning($"Could not find tag with CI-ID {tagCIID}");
                    }
                }
            }

            // extend capability map
            // reference: enrichNormalizedCiData()
            foreach (var profile in profiles.Values)
            {
                var cap = $"cap_lp_{profile.Name.ToLowerInvariant()}";

                // TODO: restrict naemons to those in list(s), see "naemons-config-generateprofiles" in yml config files (per environment)
                capMap.AddNaemons(cap, naemonInstances.Keys);
            }

            // calculate target host/service requirements/tags
            // reference: naemon-vars-ci.php in various folders
            foreach (var (ciid, hs) in evenMoreFilteredHOS)
            {
                var capCust = $"cap_cust_{hs.Customer.Nickname.ToLowerInvariant()}";
                hs.Tags.Add(capCust);

                if (hs.Profiles.Contains("profiledev-default-app-naemon-eventgenerator"))
                    hs.Tags.Add("cap_eventgenerator");

                // reference: updateNormalizedCiDataFieldProfile()
                if (hs.Profiles.Count == 1 && Regex.IsMatch(hs.Profiles.First(), "^profile", RegexOptions.IgnoreCase))
                {
                    // add legacy profile capability
                    hs.Tags.Add($"cap_lp_{hs.Profiles.First().ToLowerInvariant()}");
                }

                // reference: updateNormalizedCiData_addGenericCmdbCapTags()

                //if (array_key_exists('MONITORING_CAP', $ci['CATEGORIES']))
                //{
                //    foreach (sequentialOrAssocToSequential($ci['CATEGORIES']['MONITORING_CAP']) as $category) {
                //        array_push($ciDataRef[$id]['TAGS'], strtolower('cap_'. $category['TREE']. '_'. $category['NAME']));
                //    }
                //}
                //else
                //{
                //    array_push($ciDataRef[$id]['TAGS'], 'cap_default');
                //}


                // dynamic capability
                if (stage == Stage.PROD)
                {
                    if (hs.Profiles.Any(p => Regex.IsMatch(p, "^profiletsc-.*")))
                        hs.Tags.Add("cap_scope_tsc_yes");
                    else
                        hs.Tags.Add("cap_scope_tsc_no");
                }

                // TODO: environment-specific capabilities
            }

            // check requirements vs capabilities
            // calculate which naemons monitor which hosts/services
            // reference: enrichNormalizedCiData()
            var allNaemonCIIDs = naemonInstances.Select(i => i.Key).ToHashSet();
            var naemons2hos = new List<(Guid naemonCIID, Guid hosCIID)>();
            foreach (var (ciid, hs) in evenMoreFilteredHOS)
            {
                var naemonsAvail = new HashSet<Guid>(allNaemonCIIDs);
                foreach (var requirement in hs.Tags)
                {
                    if (Regex.IsMatch(requirement, "^cap_"))
                    {
                        if (capMap.TryGetValue(requirement, out var naemonsFulfillingRequirement))
                        {
                            naemonsAvail.IntersectWith(naemonsFulfillingRequirement);
                        }
                        else
                        {
                            naemonsAvail.Clear();
                            break;
                        }
                    }
                }
                foreach (var naemonAvail in naemonsAvail)
                    naemons2hos.Add((naemonAvail, ciid));
            }


            // write output
            var attributeFragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            var relationFragments = new List<BulkRelationDataLayerScope.Fragment>();
            // variables
            foreach (var kv in evenMoreFilteredHOS)
            {
                var debugStr = "";
                var d = new JsonObject();
                foreach (var variableGroup in kv.Value.Variables)
                {
                    var ordered = variableGroup.Value;

                    var inner = new JsonObject();
                    var first = ordered.First();
                    inner["value"] = first.Value;
                    inner["refType"] = first.RefType;

                    if (debugOutput)
                        debugStr += $"{variableGroup.Key}           {first.Value}\n";

                    var chain = new JsonArray();
                    foreach (var vv in ordered.Skip(1))
                        chain.Add(new JsonObject()
                        {
                            ["value"] = vv.Value,
                            ["refType"] = vv.RefType
                        });
                    inner["chain"] = chain;

                    d.Add(variableGroup.Key, inner);
                }
                var value = AttributeScalarValueJSON.BuildFromString(d.ToJsonString(), false);
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables", value, kv.Key));
                if (debugOutput)
                {
                    var valueDebug = new AttributeScalarValueText(debugStr, true);
                    attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables_debug", valueDebug, kv.Key));
                }
            }
            // naemons -> hosts/services
            foreach(var (naemonCIID, hosCIID) in naemons2hos)
            {
                relationFragments.Add(new BulkRelationDataLayerScope.Fragment(naemonCIID, hosCIID, "monitors", false));
            }
            // host/service tags, for debugging purposes
            foreach (var kv in evenMoreFilteredHOS)
            {
                var ciid = kv.Key;
                var v = AttributeArrayValueText.BuildFromString(kv.Value.Tags);
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_tags", v, ciid));
            }

            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataLayerScope(targetLayerID, attributeFragments),
                changesetProxy,
                new DataOriginV1(DataOriginType.ComputeLayer),
                trans,
                MaskHandlingForRemovalApplyNoMask.Instance,
                OtherLayersValueHandlingForceWrite.Instance);
            await relationModel.BulkReplaceRelations(
                new BulkRelationDataLayerScope(targetLayerID, relationFragments),
                changesetProxy,
                new DataOriginV1(DataOriginType.ComputeLayer),
                trans,
                MaskHandlingForRemovalApplyNoMask.Instance,
                OtherLayersValueHandlingForceWrite.Instance);

            return true;
        }
    }

    static class NaemonV1VariableExtensions
    {
        public static Variable ToResolvedVariable(this NaemonVariableV1 input)
        {
            return new Variable(input.name.ToUpperInvariant(), input.refType, input.value, input.precedence, input.ID);
        }
    }

    static class SelfServiceVariableExtensions
    {
        public static Variable ToResolvedVariable(this SelfServiceVariable input)
        {
            // NOTE: high precedence to make it override other variables by default
            return new Variable(input.name.ToUpperInvariant(), input.refType, input.value, precedence: 200, externalID: 0L);
        }
    }

    static class CapMapExtensions
    {
        public static void AddNaemon(this Dictionary<string, ISet<Guid>> capMap, string cap, Guid naemonCIID)
        {
            capMap.AddOrUpdate(cap,
                () => new HashSet<Guid>() { naemonCIID },
                (cur) => { cur.Add(naemonCIID); return cur; });
        }
        public static void AddNaemons(this Dictionary<string, ISet<Guid>> capMap, string cap, IEnumerable<Guid> naemonCIIDs)
        {
            capMap.AddOrUpdate(cap,
                () => new HashSet<Guid>(naemonCIIDs),
                (cur) => { cur.UnionWith(naemonCIIDs); return cur; });
        }
    }
}
