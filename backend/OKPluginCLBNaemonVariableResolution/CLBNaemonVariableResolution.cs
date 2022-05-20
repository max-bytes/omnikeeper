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
            IRelationModel relationModel, IAttributeModel attributeModel, ILatestLayerChangeModel latestLayerChangeModel) : base(latestLayerChangeModel)
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

        protected override ISet<string>? GetDependentLayerIDs(JsonDocument config, ILogger logger)
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<Configuration>(config);
                return cfg?.CMDBInputLayerSet
                    .Union(cfg?.MonmanV1InputLayerSet ?? new List<string>())
                    .Union(cfg?.SelfserviceVariablesInputLayerSet ?? new List<string>())
                    .ToHashSet();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot parse CLB config");
                return null;
            }
        }

        public override async Task<bool> Run(Layer targetLayer, JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            Configuration cfg;

            try
            {
                var tmpCfg = JsonSerializer.Deserialize<Configuration>(config);
                if (tmpCfg == null)
                    throw new Exception("Could not parse configuration");
                cfg = tmpCfg;
                logger.LogDebug("Parsed configuration for VariableRendering.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error ocurred while creating configuration instance.");
                return false;
            }

            var stage = Stage.DEV; // TODO: make configurable

            var timeThreshold = TimeThreshold.BuildLatest();

            var cmdbInputLayerset = await layerModel.BuildLayerSet(cfg.CMDBInputLayerSet.ToArray(), trans);
            var monmanV1InputLayerset = await layerModel.BuildLayerSet(cfg.MonmanV1InputLayerSet.ToArray(), trans);
            var selfserviceVariablesInputLayerset = await layerModel.BuildLayerSet(cfg.SelfserviceVariablesInputLayerSet.ToArray(), trans);

            var targetHostModel = new GenericTraitEntityModel<TargetHost, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var targetServiceModel = new GenericTraitEntityModel<TargetService, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var naemonV1VariableModel = new GenericTraitEntityModel<NaemonV1Variable, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var selfServiceVariableModel = new GenericTraitEntityModel<SelfServiceVariable, (string refType, string refID, string name)>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var customerModel = new GenericTraitEntityModel<Customer, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var profileModel = new GenericTraitEntityModel<Profile, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var categoryModel = new GenericTraitEntityModel<Category, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var serviceActionModel = new GenericTraitEntityModel<ServiceAction, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var interfaceModel = new GenericTraitEntityModel<Interface, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var groupModel = new GenericTraitEntityModel<Group, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);

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

            var relevantStatuus = new HashSet<string>()
            {
                "ACTIVE",
                "INFOALERTING",
                "BASE_INSTALLED",
                "READY_FOR_SERVICE",
                "EXPERIMENTAL",
                "HOSTING",
            };

            // construct an intermediate data structure holding hosts and services together
            var hos = new Dictionary<Guid, HostOrService>(hosts.Count + services.Count);
            foreach (var kv in hosts)
                hos.Add(kv.Key, new HostOrService(kv.Value, null));
            foreach (var kv in services)
                hos.Add(kv.Key, new HostOrService(null, kv.Value));

            // filter hosts and service
            var filteredHOS = hos
                .Where(hs => relevantStatuus.Contains(hs.Value.Status ?? ""))
                .Select(hs =>
                {
                    var profiles = hs.Value.MemberOfCategories
                        .Where(categoryCIID => cmdbProfiles.ContainsKey(categoryCIID))
                        .Select(p => cmdbProfiles[p].Name)
                        .ToList();

                    return (ciid: hs.Key, hs: hs.Value, profiles);
                })
                .ToList();
            var hosWithCategoryProfiles2CIIDLookup = filteredHOS.ToDictionary(h => h.hs.ID, h => h.ciid);

            var naemonV1Variables = await naemonV1VariableModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);

            var selfServiceVariables = await selfServiceVariableModel.GetAllByCIID(selfserviceVariablesInputLayerset, trans, timeThreshold);

            var customers = await customerModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var customerNicknameLookup = customers.ToDictionary(c => c.Value.Nickname, c => c.Value);

            var profiles = await profileModel.GetAllByDataID(monmanV1InputLayerset, trans, timeThreshold);

            var serviceActions = await serviceActionModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var serviceActionServiceIDLookup = serviceActions.Values.ToLookup(s => s.ServiceID);

            var interfaces = await interfaceModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);

            // TODO: this is not properly defined, because there can be multiple categories with the same name in different trees 
            // TODO: lookup how monman_v1 implements this and how it looks up the proper category from the profile
            // for now, we just pick the first that fits at random
            // also, we uppercase the category names
            var categoryNameLookup = categories.GroupBy(c => c.Value.Name).ToDictionary(t => t.Key.ToUpperInvariant(), t => t.First().Value);

            // get groups
            var groups = await groupModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;

            // collect variables...
            var resolvedVariables = new Dictionary<Guid, List<ResolvedVariable>>(hos.Count);

            // ...init
            // reference: updateNormalizedCiData_preProcessVars()
            foreach (var (ciid, hs, profile) in filteredHOS)
            {
                resolvedVariables.Add(ciid, new List<ResolvedVariable>()
                {
                    new ResolvedVariable("ALERTS", "FIXED", "OFF", -100),
                    new ResolvedVariable("ALERTCIID", "FIXED", hs.ID, -100),
                    new ResolvedVariable("HASNRPE", "FIXED", "YES", -100),
                    new ResolvedVariable("DYNAMICADD", "FIXED", "NO", -100),
                    new ResolvedVariable("DYNAMICMODULES", "FIXED", "YES", -100),
                });
            }

            // ...from variables in monman
            // reference: roughly updateNormalizedCiData_varsFromDatabase()
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
                        {
                            if (hosWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refHOSCIID))
                            {
                                resolvedVariables[refHOSCIID].Add(v.Value.ToResolvedVariable());
                            }
                            else
                            {
                                logger.LogWarning($"Could not find referenced CI with refID \"{refID}\" for variable \"{v.Value.ID}\", skipping variable");
                            }
                        }
                        break;
                    case "GLOBAL":
                        foreach (var rv in resolvedVariables)
                            rv.Value.Add(v.Value.ToResolvedVariable());
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
                                    if (resolvedVariables.TryGetValue(targetCIID, out var targetList))
                                    {
                                        targetList.Add(v.Value.ToResolvedVariable());
                                    }
                                    else
                                    {
                                        // member CI of category is neither host nor service, ignore
                                    }
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
                                if (resolvedVariables.TryGetValue(targetCIID, out var targetList))
                                {
                                    targetList.Add(v.Value.ToResolvedVariable());
                                }
                                else
                                {
                                    // associated CI of customer is neither host nor service, ignore
                                }
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
                        {
                            if (hosWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refHOSCIID))
                            {
                                resolvedVariables[refHOSCIID].Add(v.Value.ToResolvedVariable());
                            }
                            else
                            {
                                logger.LogWarning($"Could not find referenced CI with refID \"{refID}\" for variable with ciid \"{v.Key}\", skipping variable");
                            }
                        }
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
            foreach (var (ciid, hs, _) in filteredHOS)
            {
                var rv = resolvedVariables[ciid];

                rv.AddRange(new List<ResolvedVariable>()
                {
                    new ResolvedVariable("LOCATION", "FIXED", Regex.Replace(hs.Location ?? "", @"\p{C}+", string.Empty)),
                    new ResolvedVariable("OS", "FIXED", hs.OS ?? ""),
                    new ResolvedVariable("PLATFORM", "FIXED", hs.Platform ?? ""),
                    new ResolvedVariable("ADDRESS", "FIXED", hs.MonIPAddress ?? ""),
                    new ResolvedVariable("PORT", "FIXED", hs.MonIPPort ?? ""),
                });

                // set alerting ID to foreign key for special instances
                if (hs.Instance == "SERVER-CH")
                {
                    rv.Add(new ResolvedVariable("ALERTCIID", "FIXED", hs.ForeignKey ?? ""));
                }

                // alerts
                if (hs.Status != "ACTIVE" && hs.Status != "INFOALERTING")
                { // disable ALERTS for non-active and non-infoalerting
                    rv.Add(new ResolvedVariable("ALERTS", "FIXED", "OFF"));
                }
                else if ((hs.Environment == "DEV" || hs.Environment == "QM") && (hs.AppSupportGroup == argusGroupCIID || hs.OSSupportGroup == argusGroupCIID))
                { // disable ALERTS for DEV/QM ARGUS systems
                    rv.Add(new ResolvedVariable("ALERTS", "FIXED", "OFF"));
                }

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
                                    rv.AddRange(new List<ResolvedVariable>()
                                    {
                                        new ResolvedVariable("LOMADDRESS", "FIXED", @interface.IP ?? ""),
                                        new ResolvedVariable("LOMTYPE", "FIXED", @interface.Name?.ToUpperInvariant() ?? ""),
                                        new ResolvedVariable("LOMNAME", "FIXED", @interface.DnsName ?? ""),
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
                            {
                                rv.AddRange(new List<ResolvedVariable>()
                            {
                                new ResolvedVariable("ORACLECONNECT", "FIXED", (foundServiceAction.Command ?? "").Replace(" ", "")),
                                new ResolvedVariable("ORACLEUSER", "FIXED", foundServiceAction.CommandUser ?? ""),
                            });
                            }
                        }
                    }

                    // SD-WAN
                    if ((hs.Service.Class == "APP_ROUTING" || hs.Service.Class == "SVC_ROUTING") && Regex.IsMatch(hs.Service.Type ?? "", "^SD-WAN.*"))
                    {
                        var foundServiceAction = serviceActionServiceIDLookup[hs.Service.ID].FirstOrDefault();
                        if (foundServiceAction != null)
                        {
                            if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                            {
                                rv.AddRange(new List<ResolvedVariable>()
                            {
                                new ResolvedVariable("SDWANCHECKCONFIG", "FIXED", foundServiceAction.Command ?? ""),
                                new ResolvedVariable("SDWANORG", "FIXED", foundServiceAction.CommandUser ?? ""),
                            });
                            }
                        }
                    }
                }
            }

            // reference: updateNormalizedCiData_postProcessVars()
            foreach (var (ciid, hs, profileNames) in filteredHOS)
            {
                var rv = resolvedVariables[ciid];

                // dynamically set profile under certain circumstances
                // TODO: test
                if (profileNames.Count != 1 && hs.Host != null)
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
                        // TODO: this is not fully correct; we should instead check if the RESOLVED variable has DYNAMICADD=YES, not if there is ANY variable
                        if (rv.Any(v => v.Name == "DYNAMICADD" && v.Value == "YES"))
                        {
                            profileNames.Clear();
                            profileNames.Add("dynamic-nrpe");
                        }
                    }
                }

            //    if (
            //    (
            //        $resultRef[$id]['PROFILE'] == 'NONE' ||
            //        $resultRef[$id]['PROFILE'] == 'MULTIPLE'
            //    ) && (
            //        $resultRef[$id]['TYPE'] == 'HOST'
            //    ) && (
            //        preg_match('/^WIN/', $ci['CMDBDATA']['HOS']) ||
            //        preg_match('/^LINUX/', $ci['CMDBDATA']['HOS']) ||
            //        preg_match('/^XEN/', $ci['CMDBDATA']['HOS']) ||
            //        preg_match('/^SUNOS/', $ci['CMDBDATA']['HOS'])
            //    ) && (
            //        preg_match('/^SRV_/', $ci['CMDBDATA']['HPLATFORM'])
            //    ) && (
            //        preg_match('/ACTIVE/', $ci['CMDBDATA']['HSTATUS']) ||
            //        preg_match('/INFOALERTING/', $ci['CMDBDATA']['HSTATUS']) ||
            //        preg_match('/BASE_INSTALLED/', $ci['CMDBDATA']['HSTATUS']) ||
            //        preg_match('/READY_FOR_SERVICE/', $ci['CMDBDATA']['HSTATUS'])
            //    ) && (
            //        $resultRef[$id]['VARS']['DYNAMICADD'] == 'YES'
            //    )
            //) {
            //    $resultRef[$id]['PROFILE'] = 'dynamic-nrpe';
            //        }



                var customerNickname = "UNKNOWN";
                if (hs.Customer.HasValue && customers.TryGetValue(hs.Customer.Value, out var customer))
                    customerNickname = customer.Nickname;

                var osSupportGroupName = "UNKNOWN";
                if (hs.OSSupportGroup.HasValue && groups.TryGetValue(hs.OSSupportGroup.Value, out var osSupportGroup))
                    osSupportGroupName = osSupportGroup.Name;
                var appSupportGroupName = "UNKNOWN";
                if (hs.AppSupportGroup.HasValue && groups.TryGetValue(hs.AppSupportGroup.Value, out var appSupportGroup))
                    appSupportGroupName = appSupportGroup.Name;

                string monitoringProfile;
                if (profileNames.Count == 1)
                    monitoringProfile = profileNames[0];
                else if (profileNames.Count > 1)
                    monitoringProfile = "MULTIPLE";
                else
                    monitoringProfile = "NONE";
                string monitoringProfileOrig;
                if (profileNames.Count == 1)
                    monitoringProfileOrig = profileNames[0];
                else if (profileNames.Count > 1)
                    monitoringProfileOrig = string.Join(',', profileNames);
                else
                    monitoringProfileOrig = "NONE";

                rv.AddRange(new List<ResolvedVariable>()
                {
                    new ResolvedVariable("CIID", "FIXED", hs.ID),
                    new ResolvedVariable("CINAME", "FIXED", hs.Name ?? ""),
                    new ResolvedVariable("CONFIGSOURCE", "FIXED", "monmanagement"),

                    new ResolvedVariable("MONITORINGPROFILE", "FIXED", monitoringProfile),
                    new ResolvedVariable("MONITORINGPROFILE_ORIG", "FIXED", monitoringProfileOrig),

                    new ResolvedVariable("CUST", "FIXED", customerNickname),
                    new ResolvedVariable("CUST_ESCAPED", "FIXED", escapeCustomerNickname(customerNickname)),

                    new ResolvedVariable("ENVIRONMENT", "FIXED", hs.Environment ?? ""),
                    new ResolvedVariable("STATUS", "FIXED", hs.Status ?? ""),
                    new ResolvedVariable("CRITICALITY", "FIXED", hs.Criticality ?? ""),

                    new ResolvedVariable("SUPP_OS", "FIXED", osSupportGroupName),
                    new ResolvedVariable("SUPP_APP", "FIXED", appSupportGroupName),

                    new ResolvedVariable("INSTANCE", "FIXED", hs.Instance ?? ""),
                    new ResolvedVariable("FSOURCE", "FIXED", hs.ForeignSource ?? ""),
                    new ResolvedVariable("FKEY", "FIXED", hs.ForeignKey ?? ""),
            });
            }

            var debugOutput = false; // TODO: remove or make configurable

            // filter out hosts and services that contain an empty monitoring profile, because there is no use in creating resolved variables for them
            var filteredResolvedVariables = resolvedVariables
                .Where(kv => kv.Value.Any(v => v.Name == "MONITORINGPROFILE" && v.Value != "NONE"))
                .ToList();

            // write output
            var variableComparer = new ResolvedVariableComparer();
            var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var kv in filteredResolvedVariables)
            {
                // merge process for variables
                var groupedByName = kv.Value.GroupBy(v => v.Name)
                    .OrderBy(g => g.Key); // order by key (=variable Name) to produce stable output, otherwise JSON changes all the time, leading to "fake" changes

                var debugStr = "";
                var d = new JsonObject();
                foreach (var variablesOfCI in groupedByName)
                {
                    var ordered = variablesOfCI.OrderBy(v => v, variableComparer);

                    var inner = new JsonObject();
                    var first = ordered.First();
                    inner["value"] = first.Value;
                    inner["refType"] = first.RefType;

                    if (debugOutput)
                        debugStr += $"{variablesOfCI.Key}           {first.Value}\n";

                    var chain = new JsonArray();
                    foreach (var vv in ordered.Skip(1))
                        chain.Add(new JsonObject()
                        {
                            ["value"] = vv.Value,
                            ["refType"] = vv.RefType
                        });
                    inner["chain"] = chain;

                    d.Add(variablesOfCI.Key, inner);
                }
                var value = AttributeScalarValueJSON.BuildFromString(d.ToJsonString(), false);
                fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables", value, kv.Key));
                if (debugOutput)
                {
                    var valueDebug = new AttributeScalarValueText(debugStr, true);
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables_debug", valueDebug, kv.Key));
                }
            }

            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataLayerScope(targetLayer.ID, fragments),
                changesetProxy,
                new DataOriginV1(DataOriginType.ComputeLayer),
                trans,
                MaskHandlingForRemovalApplyNoMask.Instance,
                OtherLayersValueHandlingForceWrite.Instance);

            return true;
        }

        class ResolvedVariableComparer : IComparer<ResolvedVariable>
        {
            public int Compare(ResolvedVariable? v1, ResolvedVariable? v2)
            {
                if (v1 == null && v2 == null) return 0;
                if (v1 == null) return -1;
                if (v2 == null) return 1;
                if (v1.RefType == v2.RefType)
                {
                    if (v1.Precedence > v2.Precedence)
                        return -1;
                    else if (v1.Precedence < v2.Precedence)
                        return 1;
                    else
                    {
                        // we cannot sort "naturally", use the id as the final decider
                        if (v1.ExternalID < v2.ExternalID)
                            return -1;
                        else
                            return 1;
                    }
                }
                else
                {
                    var refType1 = RefType2Int(v1.RefType);
                    var refType2 = RefType2Int(v2.RefType);
                    if (refType1 < refType2)
                        return -1;
                    else
                        return 1;
                }
            }


            private static int RefType2Int(string refType)
            {
                return refType switch
                {
                    "FIXED" => -1,
                    "CI" => 0,
                    "PROFILE" => 1,
                    "CUST" => 2,
                    "GLOBAL" => 3,
                    _ => 4, // must not happen, other refTypes should have been filtered out by now
                };
            }
        }
    }
    class ResolvedVariable
    {
        public readonly string Name;
        public readonly string Value;
        public readonly string RefType;
        public readonly long Precedence;
        public readonly long ExternalID;

        public ResolvedVariable(string name, string refType, string value, long precedence = 0, long externalID = 0L)
        {
            Name = name;
            Value = value;
            RefType = refType;
            Precedence = precedence;
            ExternalID = externalID;
        }

        // internally, we use the "FIXED" refType, but for outputting, we replace it with "CI"
        //public string OutputRefType {
        //    get {
        //        if (RefType == "FIXED")
        //            return "CI";
        //        return RefType;
        //    }
        //}
    }

    static class NaemonV1VariableExtensions
    {
        public static ResolvedVariable ToResolvedVariable(this NaemonV1Variable input)
        {
            return new ResolvedVariable(input.name.ToUpperInvariant(), input.refType, input.value, input.precedence, input.ID);
        }
    }

    static class SelfServiceVariableExtensions
    {
        public static ResolvedVariable ToResolvedVariable(this SelfServiceVariable input)
        {
            // NOTE: high precedence to make it override other variables by default
            return new ResolvedVariable(input.name.ToUpperInvariant(), input.refType, input.value, precedence: 200, externalID: 0L);
        }
    }
}
