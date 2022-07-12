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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class CLBNaemonVariableResolution : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ILayerModel layerModel;
        private readonly IAttributeModel attributeModel;
        private readonly GenericTraitEntityModel<TargetHost, string> targetHostModel;
        private readonly GenericTraitEntityModel<TargetService, string> targetServiceModel;
        private readonly GenericTraitEntityModel<NaemonVariableV1, long> naemonV1VariableModel;
        private readonly GenericTraitEntityModel<SelfServiceVariable, (string refType, string refID, string name)> selfServiceVariableModel;
        private readonly GenericTraitEntityModel<Profile, long> profileModel;
        private readonly GenericTraitEntityModel<Category, string> categoryModel;
        private readonly GenericTraitEntityModel<ServiceAction, string> serviceActionModel;
        private readonly GenericTraitEntityModel<Interface, string> interfaceModel;
        private readonly GenericTraitEntityModel<Group, string> groupModel;
        private readonly GenericTraitEntityModel<NaemonInstanceV1, string> naemonInstanceModel;
        private readonly GenericTraitEntityModel<TagV1> tagModel;

        public CLBNaemonVariableResolution(ILayerModel layerModel, IRelationModel relationModel, IAttributeModel attributeModel,
            GenericTraitEntityModel<TargetHost, string> targetHostModel, GenericTraitEntityModel<TargetService, string> targetServiceModel, 
            GenericTraitEntityModel<NaemonVariableV1, long> naemonV1VariableModel, GenericTraitEntityModel<SelfServiceVariable, (string refType, string refID, string name)> selfServiceVariableModel,
            GenericTraitEntityModel<Profile, long> profileModel,
            GenericTraitEntityModel<Category, string> categoryModel, GenericTraitEntityModel<ServiceAction, string> serviceActionModel,
            GenericTraitEntityModel<Interface, string> interfaceModel, GenericTraitEntityModel<Group, string> groupModel,
            GenericTraitEntityModel<NaemonInstanceV1, string> naemonInstanceModel, GenericTraitEntityModel<TagV1> tagModel)
        {
            this.relationModel = relationModel;
            this.layerModel = layerModel;
            this.attributeModel = attributeModel;
            this.targetHostModel = targetHostModel;
            this.naemonV1VariableModel = naemonV1VariableModel;
            this.selfServiceVariableModel = selfServiceVariableModel;
            this.profileModel = profileModel;
            this.categoryModel = categoryModel;
            this.serviceActionModel = serviceActionModel;
            this.interfaceModel = interfaceModel;
            this.groupModel = groupModel;
            this.naemonInstanceModel = naemonInstanceModel;
            this.tagModel = tagModel;
            this.targetServiceModel = targetServiceModel;
        }

        private Configuration ParseConfig(JsonDocument configJson)
        {
            var tmpCfg = JsonSerializer.Deserialize<Configuration>(configJson, new JsonSerializerOptions()
            {
                Converters = {
                    new JsonStringEnumConverter()
                },
            });

            if (tmpCfg == null)
                throw new Exception("Could not parse configuration");
            return tmpCfg;
        }

        public override ISet<string> GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger)
        {
            var cfg = ParseConfig(config);
            return cfg.CMDBInputLayerSet
                .Union(cfg.MonmanV1InputLayerSet)
                .Union(cfg.SelfserviceVariablesInputLayerSet)
                .ToHashSet();
        }

        public override async Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, 
            JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            Configuration cfg = ParseConfig(config);

            logger.LogInformation($"Stage: {cfg.Stage}");

            string? debugTargetCMDBID = cfg.DebugTargetCMDBID;
            bool lostTraceOfDebugTargetCMDB = false;

            var timeThreshold = changesetProxy.TimeThreshold;

            var cmdbInputLayerset = await layerModel.BuildLayerSet(cfg.CMDBInputLayerSet.ToArray(), trans);
            var monmanV1InputLayerset = await layerModel.BuildLayerSet(cfg.MonmanV1InputLayerSet.ToArray(), trans);
            var selfserviceVariablesInputLayerset = await layerModel.BuildLayerSet(cfg.SelfserviceVariablesInputLayerSet.ToArray(), trans);

            var instanceRules = cfg.Stage switch
            {
                Stage.Dev => (IInstanceRules)new InstanceRulesDev(),
                Stage.Prod => new InstanceRulesProd(),
                _ => throw new NotImplementedException(),
            };

            var allCategories = await categoryModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var hosts = await targetHostModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var services = await targetServiceModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var naemonV1Variables = await naemonV1VariableModel.GetByCIID(AllCIIDsSelection.Instance, monmanV1InputLayerset, trans, timeThreshold);
            var selfServiceVariables = await selfServiceVariableModel.GetByCIID(AllCIIDsSelection.Instance, selfserviceVariablesInputLayerset, trans, timeThreshold);
            var profiles = await profileModel.GetByDataID(AllCIIDsSelection.Instance, monmanV1InputLayerset, trans, timeThreshold);
            var serviceActions = await serviceActionModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var interfaces = await interfaceModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var groups = await groupModel.GetByCIID(AllCIIDsSelection.Instance, cmdbInputLayerset, trans, timeThreshold);
            var naemonInstances = await naemonInstanceModel.GetByCIID(AllCIIDsSelection.Instance, monmanV1InputLayerset, trans, timeThreshold);
            var tags = await tagModel.GetByCIID(AllCIIDsSelection.Instance, monmanV1InputLayerset, trans, timeThreshold);

            // filter cmdb profiles
            var cmdbProfiles = allCategories
                .Where(kv => instanceRules.FilterProfileFromCmdbCategory(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value);

            // filter naemon instances
            var filteredNaemonInstances = naemonInstances.Where(kv => instanceRules.FilterNaemonInstance(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value);

            // construct an intermediate data structure holding hosts and services together
            List<string> CalculateCIProfiles(Guid[] memberOfCategories) => 
                memberOfCategories
                .Where(categoryCIID => cmdbProfiles.ContainsKey(categoryCIID))
                .Select(p => cmdbProfiles[p].Name)
                .ToList(); 
            List<Category> CalculateCategories(Guid[] memberOfCategories) =>
                memberOfCategories
                .Where(categoryCIID => allCategories.ContainsKey(categoryCIID))
                .Select(p => allCategories[p])
                .ToList();

            var hos = new Dictionary<Guid, HostOrService>(hosts.Count + services.Count);
            foreach (var kv in hosts)
            {
                var profilesOfCI = CalculateCIProfiles(kv.Value.MemberOfCategories);
                var categoriesOfCI = CalculateCategories(kv.Value.MemberOfCategories);
                hos.Add(kv.Key, new HostOrService(kv.Value, null, profilesOfCI, categoriesOfCI));
            }
            foreach (var kv in services)
            {
                var profilesOfCI = CalculateCIProfiles(kv.Value.MemberOfCategories);
                var categoriesOfCI = CalculateCategories(kv.Value.MemberOfCategories);
                hos.Add(kv.Key, new HostOrService(null, kv.Value, profilesOfCI, categoriesOfCI));
            }

            if (!lostTraceOfDebugTargetCMDB && debugTargetCMDBID != null)
            {
                if (hos.Any(hs => hs.Value.ID == debugTargetCMDBID))
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} exists");
                else
                {
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} does NOT exist!");
                    lostTraceOfDebugTargetCMDB = true;
                }
            }

            // filter hosts and services
            var filteredHOS = hos.Where(kv => instanceRules.FilterTarget(kv.Value) && instanceRules.FilterCustomer(kv.Value.CustomerNickname)).ToDictionary(kv => kv.Key, kv => kv.Value);

            if (!lostTraceOfDebugTargetCMDB && debugTargetCMDBID != null)
            {
                if (filteredHOS.Any(hs => hs.Value.ID == debugTargetCMDBID))
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was not filtered out by instance-rules");
                else
                {
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was filtered out by instance-rules");
                    lostTraceOfDebugTargetCMDB = true;
                }
            }


            // reference: updateNormalizedCiDataFieldProfile()
            foreach (var kv in filteredHOS)
            {
                var hs = kv.Value;
                if (hs.Profiles.Count == 1 && hs.HasProfileMatchingRegex("^profile", RegexOptions.IgnoreCase))
                {
                    // add legacy profile capability
                    hs.Tags.Add($"cap_lp_{hs.Profiles.First().ToLowerInvariant()}");
                }
            }

            // calculate target host/service requirements/tags
            // reference: updateNormalizedCiData_addGenericCmdbCapTags()
            foreach (var (ciid, hs) in filteredHOS)
            {
                var hasMonitoringCapCategory = false;
                foreach (var category in hs.Categories.Where(c => c.Group == "MONITORING_CAP"))
                {
                    var tag = $"cap_{category.Tree}_{category.Name}".ToLowerInvariant();
                    hs.Tags.Add(tag);
                    hasMonitoringCapCategory = true;
                }
                if (!hasMonitoringCapCategory)
                {
                    hs.Tags.Add("cap_default");
                }
            }

            // collect variables...

            // ...init
            // reference: updateNormalizedCiData_preProcessVars()
            foreach (var (ciid, hs) in filteredHOS)
            {
                hs.AddVariables(
                    new Variable("ALERTS", "INIT", "OFF", -100),
                    new Variable("ALERTCIID", "INIT", hs.ID, -100),
                    new Variable("HASNRPE", "INIT", "YES", -100),
                    new Variable("DYNAMICADD", "INIT", "NO", -100),
                    new Variable("DYNAMICMODULES", "INIT", "YES", -100)
                );
            }

            foreach (var (ciid, hs) in filteredHOS)
            {
                hs.AddVariables(
                    new Variable("TICKETTARGET", "INIT", "OS", -100) // TODO: is this feasible?
                );
            }

            // ...from variables in monman
            // reference: roughly updateNormalizedCiData_varsFromDatabase()
            var hosByNameLookup = filteredHOS.ToDictionary(h => h.Value.ID, h => h.Value);
            var hosByCustomerNicknameLookup = filteredHOS.ToLookup(h => h.Value.CustomerNickname, h => h.Value);

            // HACK: this is not properly defined, because there can be multiple categories with the same name in different trees/groups
            // and even multiple categories with the same name in the same tree and group
            // for now, we just pick the first that fits at random
            // also, we uppercase the category names
            var categoryNameLookup = allCategories.Where(kv => kv.Value.Instance == "SERVER").GroupBy(kv => kv.Value.Name.ToUpperInvariant()).ToDictionary(g => g.Key, g => g.First().Value);
            //var categoryNameLookup = categories.GroupBy(c => c.Value.Name).ToDictionary(t => t.Key.ToUpperInvariant(), t => t.First().Value);
            //var categoryNameLookup = cmdbProfiles.ToDictionary(kv => kv.Value.Name.ToUpperInvariant(), kv => kv.Value);
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
                            issueAccumulator.TryAdd("variable_cant_find_referenced_ci", v.Value.ID.ToString(), $"Could not find referenced CI with refID \"{refID}\" or referenced CI does not meet criteria; for variable \"{v.Value.ID}\", skipping", v.Key);
                        break;
                    case "GLOBAL":
                        foreach (var rv in filteredHOS)
                            rv.Value.AddVariable(v.Value.ToResolvedVariable());
                        break;
                    case "PROFILE":
                        // approach: get the profile, look up its name, then fetch the corresponding CMDB category, then its member CIs
                        if (!long.TryParse(refID, out var refIDProfile))
                        {
                            issueAccumulator.TryAdd("variable_cant_parse_referenced_profile", v.Value.ID.ToString(), $"Could not parse refID \"{refID}\" into number to look up profile", v.Key);
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
                                    {
                                        if (hs.Profiles.Count == 1)
                                        { 
                                            // NOTE: we only apply profile variables to CIs that have a single profile, not multiple, not none
                                            // that seems to be consistent with the old implementation
                                            hs.AddVariable(v.Value.ToResolvedVariable());
                                        }
                                    }
                                    else { } // member CI of category is neither host nor service, ignore
                                }
                            } 
                            //else if (categoryInstanceAndNameLookup.TryGetValue(("SERVER-CH", profileName), out var categoryCH))
                            //{
                            //    foreach (var targetCIID in categoryCH.Members)
                            //    {
                            //        if (filteredHOS.TryGetValue(targetCIID, out var hs))
                            //            hs.AddVariable(v.Value.ToResolvedVariable());
                            //        else { } // member CI of category is neither host nor service, ignore
                            //    }
                            //}
                            else
                            {
                                issueAccumulator.TryAdd("variable_cant_find_referenced_profile_as_category", v.Value.ID.ToString(), $"Could not find category with name \"{profileName}\", skipping variable", v.Key);
                            }
                        }
                        else
                        {
                            issueAccumulator.TryAdd("variable_cant_find_referenced_profile", v.Value.ID.ToString(), $"Could not find referenced profile with refID \"{refIDProfile}\" for variable \"{v.Value.ID}\", skipping variable", v.Key);
                        }
                        break;
                    case "CUST":
                        var targetHOS = hosByCustomerNicknameLookup[refID];
                        foreach (var targetHS in targetHOS)
                        {
                            targetHS.AddVariable(v.Value.ToResolvedVariable());
                        }
                        break;
                    default:
                        issueAccumulator.TryAdd("variable_invalid_ref_type", v.Value.ID.ToString(), $"Could not process monman variable \"{v.Value.ID}\": invalid refType \"{v.Value.refType}\"", v.Key);
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
                            issueAccumulator.TryAdd("selfServiceVariable", v.Key.ToString(), $"Could not find referenced CI with refID \"{refID}\" for variable with ciid \"{v.Key}\", skipping variable", v.Key);
                        break;
                    default:
                        issueAccumulator.TryAdd("selfServiceVariable", v.Key.ToString(), $"Could not process selfservice variable with ciid \"{v.Key}\": invalid refType \"{v.Value.refType}\"", v.Key);
                        break;
                }
            }

            // ...from cmdb hosts and services
            // reference: roughly updateNormalizedCiData_varsByExpression()
            var serviceActionServiceIDLookup = serviceActions.Values.ToLookup(s => s.ServiceID);
            foreach (var (ciid, hs) in filteredHOS)
            {
                hs.AddVariables(
                    new Variable("OS", "FIXED", hs.OS ?? ""),
                    new Variable("PLATFORM", "FIXED", hs.Platform ?? ""),
                    new Variable("ADDRESS", "FIXED", hs.MonIPAddress ?? ""),
                    new Variable("PORT", "FIXED", hs.MonIPPort ?? "")
                );

                // location
                // reference: updateNormalizedCiData_updateLocationField()
                var location = hs.Location;
                if (location == null || location.Length == 0)
                {
                    var numSteps = 0;
                    var current = hs;
                    while (numSteps < 10)
                    {
                        if (current.Service != null)
                        { // NOTE: we only do this for services; the old code doesn't explicitly say that it only does it for services, but implicitly, it does
                            // try to find location via runsOn relation
                            if (!current.RunsOn.IsEmpty())
                            {
                                // NOTE: we lookup in ALL hosts/services, not just the filtered ones
                                if (hos.TryGetValue(current.RunsOn[0], out var parentHS)) // NOTE, TODO: we pick the first runsOn relation we find, might lead to inconsistent results
                                {
                                    current = parentHS;
                                } else
                                {
                                    // couldn't find its parent host/service, bail
                                    break;
                                }
                            } else
                            {
                                // current service does not run on anything, bail
                                break;
                            }
                        } else
                        {
                            // reached a host
                            location = current.Location;
                            break;
                        }
                        numSteps++;
                    }
                }
                hs.AddVariables(
                    new Variable("LOCATION", "FIXED", Regex.Replace(location ?? "", @"\p{C}+", string.Empty))
                );

                // set alerting ID to foreign key for special instances
                if (hs.Instance == "SERVER-CH")
                    hs.AddVariable(new Variable("ALERTCIID", "FIXED", hs.ForeignKey ?? ""));

                // host-specific stuff
                // reference: naemon-vars-ci.php
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
                                    hs.AddVariables(
                                        new Variable("LOMADDRESS", "FIXED", @interface.IP ?? ""),
                                        new Variable("LOMTYPE", "FIXED", @interface.Name?.ToUpperInvariant() ?? ""),
                                        new Variable("LOMNAME", "FIXED", @interface.DnsName ?? "")
                                    );
                                    break;
                                }
                            }
                        }
                    }
                }

                // service-specific stuff
                // reference: naemon-vars-ci.php
                if (hs.Service != null)
                {
                    // extract oracle db connection string
                    if (Regex.IsMatch(hs.Service.Class ?? "", "DB") && Regex.IsMatch(hs.Service.Type ?? "", "ORACLE"))
                    {
                        var foundServiceAction = serviceActionServiceIDLookup[hs.Service.ID].FirstOrDefault();
                        if (foundServiceAction != null)
                        {
                            if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                                hs.AddVariables(
                                    new Variable("ORACLECONNECT", "FIXED", (foundServiceAction.Command ?? "").Replace(" ", "")),
                                    new Variable("ORACLEUSER", "FIXED", foundServiceAction.CommandUser ?? "")
                                );
                        }
                    }

                    // SD-WAN
                    if ((hs.Service.Class == "APP_ROUTING" || hs.Service.Class == "SVC_ROUTING") && Regex.IsMatch(hs.Service.Type ?? "", "^SD-WAN.*"))
                    {
                        var foundServiceAction = serviceActionServiceIDLookup[hs.Service.ID].FirstOrDefault();
                        if (foundServiceAction != null)
                        {
                            if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                                hs.AddVariables(
                                    new Variable("SDWANCHECKCONFIG", "FIXED", foundServiceAction.Command ?? ""),
                                    new Variable("SDWANORG", "FIXED", foundServiceAction.CommandUser ?? "")
                                );
                        }
                    }

                    // TSI Silverpeak
                    if (hs.Service.Instance == "SDWAN-INT")
                    {
                        if (hs.Service.Class == "APP_MGMT" && hs.Service.Type == "SILVERPEAK_ORCHESTRATOR")
                        {
                            hs.Tags.Add("cap_tsi_silverpeak");
                            hs.Profiles = new List<string>() { "profiledynamic-tsi-silverpeak-orchestrator" };
                        }
                        if (hs.Service.Class == "APP_ROUTING" && hs.Service.Type == "SILVERPEAK_DEVICE")
                        {
                            hs.Tags.Add("cap_tsi_silverpeak");
                            hs.Profiles = new List<string>() { "profiledynamic-tsi-silverpeak-device" };
                        }
                    }

                    // TSI Versa
                    if (hs.Service.Instance == "SDWAN-INT")
                    {
                        if (hs.Service.Class == "APP_MGMT" && hs.Service.Type == "VERSA_ORCHESTRATOR")
                        {
                            hs.Tags.Add("cap_tsi_versa");
                            hs.Profiles = new List<string>() { "profiledynamic-tsi-versa-orchestrator" };
                        }
                        if (hs.Service.Class == "APP_MGMT" && hs.Service.Type == "VERSA_DIRECTOR")
                        {
                            hs.Tags.Add("cap_tsi_versa");
                            hs.Profiles = new List<string>() { "profiledynamic-tsi-versa-orchestrator" };
                        }
                        if (hs.Service.Class == "APP_ROUTING" && hs.Service.Type == "VERSA_DEVICE")
                        {
                            hs.Tags.Add("cap_tsi_versa");
                            hs.Profiles = new List<string>() { "profiledynamic-tsi-versa-device" };
                        }
                    }
                }

                // instance rules
                instanceRules.ApplyInstanceRules(hs, groups);
            }

            // reference: updateNormalizedCiData_postProcessVars()
            foreach (var (ciid, hs) in filteredHOS)
            {
                // ensure dynamic module injection off for ping-only patterns
                if (hs.HasProfileMatchingRegex("ping-only", RegexOptions.IgnoreCase))
                {
                    hs.AddVariable(new Variable("DYNAMICMODULES", "FIXED", "NO"));
                }

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

                var customerNickname = hs.CustomerNickname;

                var osSupportGroupName = "00EMPTY";
                if (hs.OSSupportGroup.HasValue && groups.TryGetValue(hs.OSSupportGroup.Value, out var osSupportGroup))
                    osSupportGroupName = osSupportGroup.Name;
                var appSupportGroupName = "00EMPTY";
                if (hs.AppSupportGroup.HasValue && groups.TryGetValue(hs.AppSupportGroup.Value, out var appSupportGroup))
                    appSupportGroupName = appSupportGroup.Name;

                string monitoringProfile;
                if (hs.Profiles.Count == 1)
                    monitoringProfile = hs.Profiles[0].ToLowerInvariant(); // HACK: we transform to lowercase for historical reasons
                else if (hs.Profiles.Count > 1)
                    monitoringProfile = "MULTIPLE";
                else
                    monitoringProfile = "NONE";
                string monitoringProfileOrig = string.Join(',', hs.ProfilesOrig).ToLowerInvariant(); // HACK: we transform to lowercase for historical reasons

                hs.AddVariables(
                    new Variable("CIID", "FIXED", hs.ID),
                    new Variable("CINAME", "FIXED", hs.Name ?? ""),
                    new Variable("CONFIGSOURCE", "FIXED", "monmanagement"),

                    new Variable("MONITORINGPROFILE", "FIXED", monitoringProfile),
                    new Variable("MONITORINGPROFILE_ORIG", "FIXED", monitoringProfileOrig),

                    new Variable("CUST", "FIXED", customerNickname),
                    new Variable("CUST_ESCAPED", "FIXED", EscapeCustomerNickname(customerNickname)),

                    new Variable("ENVIRONMENT", "FIXED", hs.Environment ?? ""),
                    new Variable("STATUS", "FIXED", hs.Status ?? ""),
                    new Variable("CRITICALITY", "FIXED", hs.Criticality ?? ""),

                    new Variable("SUPP_OS", "FIXED", osSupportGroupName),
                    new Variable("SUPP_APP", "FIXED", appSupportGroupName),

                    new Variable("INSTANCE", "FIXED", hs.Instance ?? ""),
                    new Variable("FSOURCE", "FIXED", hs.ForeignSource ?? ""),
                    new Variable("FKEY", "FIXED", hs.ForeignKey ?? "")
                );
            }

            // reference: getNaemonConfigObjectsFromTSISilverpeakTemplates()
            foreach (var (ciid, hs) in filteredHOS)
            {
                if (hs.HasProfile(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-silverpeak-orchestrator"))
                {
                    hs.AddVariable(new Variable("ORGNAME", "FIXED", hs.CustomerNickname));
                }
                if (hs.HasProfile(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-silverpeak-device"))
                {
                    hs.AddVariable(new Variable("ORGNAME", "FIXED", hs.CustomerNickname));
                    hs.AddVariable(new Variable("DEVICENAME", "FIXED", hs.Name ?? ""));
                }
            }

            // reference: getNaemonConfigObjectsFromTSIVersaTemplates()
            foreach (var (ciid, hs) in filteredHOS)
            {
                if (hs.HasProfile(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-versa-orchestrator"))
                {
                    hs.AddVariable(new Variable("ORGNAME", "FIXED", hs.CustomerNickname));
                }
                if (hs.HasProfile(StringComparison.InvariantCultureIgnoreCase, "profiledynamic-tsi-versa-device"))
                {
                    hs.AddVariable(new Variable("ORGNAME", "FIXED", hs.CustomerNickname));
                    hs.AddVariable(new Variable("DEVICENAME", "FIXED", hs.Name ?? ""));
                }
            }

            // filter out hosts and services that contain no monitoring profile, because there is no use in creating resolved variables for them
            var evenMoreFilteredHOS = filteredHOS
                .Where(kv => kv.Value.Profiles.Count != 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (!lostTraceOfDebugTargetCMDB && debugTargetCMDBID != null)
            {
                if (evenMoreFilteredHOS.Any(hs => hs.Value.ID == debugTargetCMDBID))
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was not filtered out by profile-count check");
                else
                {
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was filtered out by profile-count check");
                    lostTraceOfDebugTargetCMDB = true;
                }
            }

            // calculate "uses" per host/service
            // reference:  naemon-templates-objects.php
            foreach (var (ciid, hs) in evenMoreFilteredHOS)
            {
                if (hs.Profiles.Count == 1 && hs.HasProfile(StringComparison.InvariantCultureIgnoreCase, "dynamic-nrpe"))
                {
                    hs.UseDirective.Add("global-variables");
                    hs.UseDirective.Add("tsa-generic-host");
                } 
                else if (hs.Profiles.Count > 1)
                {
                    hs.UseDirective.Add("global-variables");
                    hs.UseDirective.Add("tsa-generic-host");
                } 
                else
                {
                    if (!hs.Profiles.IsEmpty())
                    {
                        // NOTE: we can be sure the host/service has at least one profile, because we filtered out those without any profiles before
                        hs.UseDirective.Add(hs.Profiles[0].ToLowerInvariant()); // HACK: we transform to lowercase for historical reasons
                    }
                }
            }

            // build capability map 
            // reference: getCapabilityMap()
            var capMap = new Dictionary<string, ISet<Guid>>(); // key: capability name, value: list of CIIDs of naemon instances
            var invertedCapMap = new Dictionary<Guid, ISet<string>>(); // key: naemon ciid, value: list of capabilities
            foreach (var kv in filteredNaemonInstances)
            {
                var naemonCIID = kv.Key;
                var naemon = kv.Value;
                foreach (var tagCIID in naemon.Tags)
                {
                    if (tags.TryGetValue(tagCIID, out var tag))
                    {
                        if (Regex.IsMatch(tag.Name, "^cap_"))
                        {
                            capMap.AddNaemon(tag.Name, naemonCIID);
                            invertedCapMap.AddCapability(naemonCIID, tag.Name);
                        }
                    }
                    else
                    {
                        issueAccumulator.TryAdd("tag", tagCIID.ToString(), $"Could not find tag with CI-ID {tagCIID}", tagCIID);
                    }
                }
            }

            // extend capability map
            // reference: enrichNormalizedCiData()
            foreach (var profile in profiles.Values)
            {
                var cap = $"cap_lp_{profile.Name.ToLowerInvariant()}";
                capMap.AddNaemons(cap, filteredNaemonInstances.Keys);
                foreach (var naemonInstanceCII in filteredNaemonInstances.Keys)
                    invertedCapMap.AddCapability(naemonInstanceCII, cap);
            }

            // check requirements vs capabilities
            // calculate which naemons monitor which hosts/services
            // reference: enrichNormalizedCiData()
            var allNaemonCIIDs = filteredNaemonInstances.Select(i => i.Key).ToHashSet();
            var naemons2hos = new List<(Guid naemonCIID, Guid hosCIID)>();
            var targetsWithAtLeastOneNaemonInstanceMonitoringIt = new HashSet<Guid>();
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
                if (!naemonsAvail.IsEmpty())
                {
                    targetsWithAtLeastOneNaemonInstanceMonitoringIt.Add(ciid);

                    foreach (var naemonAvail in naemonsAvail)
                            naemons2hos.Add((naemonAvail, ciid));
                }
            }

            // filter out targets which are not monitored by any naemon instance
            // TODO: really necessary?
            var extremelyFilteredHOS = evenMoreFilteredHOS;//.Where(kv => targetsWithAtLeastOneNaemonInstanceMonitoringIt.Contains(kv.Key)).ToList();

            if (!lostTraceOfDebugTargetCMDB && debugTargetCMDBID != null)
            {
                if (extremelyFilteredHOS.Any(hs => hs.Value.ID == debugTargetCMDBID))
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was not filtered out by at-least-one-naemon-instance-monitoring-it check");
                else
                {
                    logger.LogInformation($"Debug Target {debugTargetCMDBID} was filtered out by at-least-one-naemon-instance-monitoring-it check");
                    lostTraceOfDebugTargetCMDB = true;
                }
            }

            // write output
            var attributeFragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            var relationFragments = new List<BulkRelationDataLayerScope.Fragment>();
            // variables
            foreach (var kv in extremelyFilteredHOS)
            {
                var d = new JsonObject();
                foreach (var variableGroup in kv.Value.Variables.OrderBy(kv => kv.Key))
                {
                    var ordered = variableGroup.Value;

                    var inner = new JsonObject();
                    var first = ordered.First();
                    // NOTE: naemon/thruk seem to trim variable values anyway, so we do that here too, to produce better comparable results
                    inner["value"] = first.Value.Trim();
                    inner["refType"] = first.RefType;

                    if (first.IsSecret)
                    { // NOTE: only add isSecret flag if variable is actually a secret, to keep the JSON small
                        inner["isSecret"] = true;
                    }

                    var chain = new JsonArray();
                    foreach (var vv in ordered.Skip(1))
                        chain.Add(new JsonObject()
                        {
                            // NOTE: naemon/thruk seem to trim variable values anyway, so we do that here too, to produce better comparable results
                            ["value"] = vv.Value.Trim(),
                            ["refType"] = vv.RefType
                        });
                    inner["chain"] = chain;

                    d.Add(variableGroup.Key, inner);
                }
                var value = AttributeScalarValueJSON.BuildFromString(d.ToJsonString(), false);
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables", value, kv.Key));
            }
            // naemons -> hosts/services
            foreach(var (naemonCIID, hosCIID) in naemons2hos)
            {
                relationFragments.Add(new BulkRelationDataLayerScope.Fragment(naemonCIID, hosCIID, "monitors", false));
            }
            // uses
            foreach (var kv in extremelyFilteredHOS)
            {
                var ciid = kv.Key;
                var v = AttributeArrayValueText.BuildFromString(kv.Value.UseDirective);
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.use_directive", v, ciid));
            }
            // host/service tags, for debugging purposes
            foreach (var kv in evenMoreFilteredHOS)
            {
                var ciid = kv.Key;
                var v = AttributeArrayValueText.BuildFromString(kv.Value.Tags.OrderBy(t => t));
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_tags", v, ciid));
            }
            // naemon capabilities, for debugging purposes
            foreach(var kv in invertedCapMap)
            {
                var ciid = kv.Key;
                var v = AttributeArrayValueText.BuildFromString(kv.Value.OrderBy(t => t));
                attributeFragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.capabilities", v, ciid));
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

        private string EscapeCustomerNickname(string customerNickname)
        {
            return customerNickname
                .Replace(' ', '-')
                .Replace('(', '-')
                .Replace(')', '-')
                .Replace('[', '-')
                .Replace(']', '-');
        }
    }

    static class NaemonV1VariableExtensions
    {
        public static Variable ToResolvedVariable(this NaemonVariableV1 input)
        {
            return new Variable(input.name.ToUpperInvariant(), input.refType, input.value, input.precedence, input.ID, input.isSecret);
        }
    }

    static class SelfServiceVariableExtensions
    {
        public static Variable ToResolvedVariable(this SelfServiceVariable input)
        {
            // NOTE: high precedence to make it override other variables by default
            return new Variable(input.name.ToUpperInvariant(), input.refType, input.value, precedence: 200, externalID: 0L, isSecret: false);
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

    static class InvertedCapMapExtensions
    {
        public static void AddCapability(this Dictionary<Guid, ISet<string>> invertedCapMap, Guid naemonCIID, string cap)
        {
            invertedCapMap.AddOrUpdate(naemonCIID,
                () => new HashSet<string>() { cap },
                (cur) => { cur.Add(cap); return cur; });
        }
    }
}
