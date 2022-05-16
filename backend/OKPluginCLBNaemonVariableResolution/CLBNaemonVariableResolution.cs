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

            var timeThreshold = TimeThreshold.BuildLatest();

            var cmdbInputLayerset = await layerModel.BuildLayerSet(cfg.CMDBInputLayerSet.ToArray(), trans);
            var monmanV1InputLayerset = await layerModel.BuildLayerSet(cfg.MonmanV1InputLayerSet.ToArray(), trans);
            var selfserviceVariablesInputLayerset = await layerModel.BuildLayerSet(cfg.SelfserviceVariablesInputLayerSet.ToArray(), trans);

            var hostTraitModel = new GenericTraitEntityModel<TargetHost, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var serviceTraitModel = new GenericTraitEntityModel<TargetService, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var naemonV1VariableModel = new GenericTraitEntityModel<NaemonV1Variable, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var selfServiceVariableModel = new GenericTraitEntityModel<SelfServiceVariable, (string refType, string refID, string name)>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var customerTraitModel = new GenericTraitEntityModel<Customer, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var profileTraitModel = new GenericTraitEntityModel<Profile, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var categoryTraitModel = new GenericTraitEntityModel<Category, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var serviceActionTraitModel = new GenericTraitEntityModel<ServiceAction, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var interfaceTraitModel = new GenericTraitEntityModel<Interface, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var groupTraitModel = new GenericTraitEntityModel<Group, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);

            var categories = await categoryTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);

            var cmdbProfiles = categories
                .Where(kv => kv.Value.Group == "MONITORING")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // extract cmdb hosts, but limit to those that have a monitoring profile (=related to a proper cmdb category)
            var hosts = await hostTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var hostsWithCategoryProfiles = hosts
                .Select(host =>
                {
                    var profileCIID = host.Value.MemberOfCategories.FirstOrDefault(categoryCIID => cmdbProfiles.ContainsKey(categoryCIID));

                    if (profileCIID == default)
                    {
                        return (host.Key, host.Value, null);
                    }
                    else
                    {
                        var profile = cmdbProfiles[profileCIID];
                        return (ciid: host.Key, host: host.Value, profile: (Category?)profile);
                    }
                })
                .Where(t => t.profile != null)
                .Select(t => (t.ciid, t.host, t.profile!))
                .ToList();
            var hostsWithCategoryProfiles2CIIDLookup = hostsWithCategoryProfiles.ToDictionary(h => h.host.ID, h => h.ciid);

            // extract cmdb services, but limit to those that have a monitoring profile (=related to a proper cmdb category)
            var services = await serviceTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var servicesWithCategoryProfiles = services
                .Select(service =>
                {
                    var profileCIID = service.Value.MemberOfCategories.FirstOrDefault(categoryCIID => cmdbProfiles.ContainsKey(categoryCIID));

                    if (profileCIID == default)
                    {
                        return (service.Key, service.Value, null);
                    }
                    else
                    {
                        var profile = cmdbProfiles[profileCIID];
                        return (ciid: service.Key, service: service.Value, profile: (Category?)profile);
                    }
                })
                .Where(t => t.profile != null)
                .Select(t => (t.ciid, t.service, t.profile!))
                .ToList();
            var servicesWithCategoryProfiles2CIIDLookup = servicesWithCategoryProfiles.ToDictionary(h => h.service.ID, h => h.ciid);

            var naemonV1Variables = await naemonV1VariableModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);

            var selfServiceVariables = await selfServiceVariableModel.GetAllByCIID(selfserviceVariablesInputLayerset, trans, timeThreshold);

            var customers = await customerTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var customerNicknameLookup = customers.ToDictionary(c => c.Value.Nickname, c => c.Value);

            var profiles = await profileTraitModel.GetAllByDataID(monmanV1InputLayerset, trans, timeThreshold);

            var serviceActions = await serviceActionTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var serviceActionServiceIDLookup = serviceActions.Values.ToLookup(s => s.ServiceID);

            var interfaces = await interfaceTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);

            // TODO: this is not properly defined, because there can be multiple categories with the same name in different trees 
            // TODO: lookup how monman_v1 implements this and how it looks up the proper category from the profile
            // for now, we just pick the first that fits at random
            // also, we uppercase the category names
            var categoryNameLookup = categories.GroupBy(c => c.Value.Name).ToDictionary(t => t.Key.ToUpperInvariant(), t => t.First().Value);

            // get groups
            var groups = await groupTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var argusGroupCIID = groups.FirstOrDefault(kv => kv.Value.Name == "GDE.PEA.AT.ALL.ARGUS").Key;

            // collect variables...
            var resolvedVariables = new Dictionary<Guid, List<ResolvedVariable>>(hostsWithCategoryProfiles.Count + servicesWithCategoryProfiles.Count);
            foreach (var kv in hostsWithCategoryProfiles)
                resolvedVariables.TryAdd(kv.ciid, new List<ResolvedVariable>());
            foreach (var kv in servicesWithCategoryProfiles)
                resolvedVariables.TryAdd(kv.ciid, new List<ResolvedVariable>());

            // ...from variables in monman
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
                            if (hostsWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refHostCIID))
                            {
                                resolvedVariables[refHostCIID].Add(v.Value.ToResolvedVariable());
                            }
                            else if (servicesWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refServiceCIID))
                            {
                                resolvedVariables[refServiceCIID].Add(v.Value.ToResolvedVariable());
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
                            if (hostsWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refHostCIID))
                            {
                                resolvedVariables[refHostCIID].Add(v.Value.ToResolvedVariable());
                            }
                            else if (servicesWithCategoryProfiles2CIIDLookup.TryGetValue(refID, out var refServiceCIID))
                            {
                                resolvedVariables[refServiceCIID].Add(v.Value.ToResolvedVariable());
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

            // ...from cmdb hosts
            foreach (var (ciid, host, profile) in hostsWithCategoryProfiles)
            {
                var rv = resolvedVariables[ciid];

                // base 
                var customerNickname = "UNKNOWN";
                if (host.Customer.HasValue && customers.TryGetValue(host.Customer.Value, out var customer))
                    customerNickname = customer.Nickname;

                var osSupportGroupName = "UNKNOWN";
                if (host.OSSupportGroup.HasValue && groups.TryGetValue(host.OSSupportGroup.Value, out var osSupportGroup))
                    osSupportGroupName = osSupportGroup.Name;
                var appSupportGroupName = "UNKNOWN";
                if (host.AppSupportGroup.HasValue && groups.TryGetValue(host.AppSupportGroup.Value, out var appSupportGroup))
                    appSupportGroupName = appSupportGroup.Name;

                rv.AddRange(new List<ResolvedVariable>()
                {
                    new ResolvedVariable("HASNRPE", "FORCED", "YES"),
                    new ResolvedVariable("DYNAMICADD", "FORCED", "NO"),
                    new ResolvedVariable("DYNAMICMODULES", "FORCED", "YES"),

                    new ResolvedVariable("ENVIRONMENT", "FORCED", host.Environment ?? ""),
                    new ResolvedVariable("STATUS", "FORCED", host.Status ?? ""),
                    new ResolvedVariable("CRITICALITY", "FORCED", host.Criticality ?? ""),

                    new ResolvedVariable("LOCATION", "FORCED", Regex.Replace(host.Location ?? "", @"\p{C}+", string.Empty)),
                    new ResolvedVariable("OS", "FORCED", host.OS ?? ""),
                    new ResolvedVariable("PLATFORM", "FORCED", host.Platform ?? ""),
                    new ResolvedVariable("ADDRESS", "FORCED", host.MonIPAddress ?? ""),
                    new ResolvedVariable("PORT", "FORCED", host.MonIPPort ?? ""),

                    new ResolvedVariable("CIID", "FORCED", host.ID),
                    new ResolvedVariable("CINAME", "FORCED", host.Hostname ?? ""),
                    new ResolvedVariable("CONFIGSOURCE", "FORCED", "monmanagement"),

                    new ResolvedVariable("CUST", "FORCED", customerNickname),
                    new ResolvedVariable("CUST_ESCAPED", "FORCED", escapeCustomerNickname(customerNickname)),

                    new ResolvedVariable("INSTANCE", "FORCED", host.Instance ?? ""),
                    new ResolvedVariable("FSOURCE", "FORCED", host.ForeignSource ?? ""),
                    new ResolvedVariable("FKEY", "FORCED", host.ForeignKey ?? ""),

                    new ResolvedVariable("SUPP_OS", "FORCED", osSupportGroupName),
                    new ResolvedVariable("SUPP_APP", "FORCED", appSupportGroupName),

                    new ResolvedVariable("MONITORINGPROFILE", "FORCED", profile.Name),
                });

                // TODO
                //$resultRef[$id]['VARS']['MONITORINGPROFILE_ORIG'] = join(',', $resultRef[$id]['PROFILE_ORIG']);

                // hardware monitoring
                foreach (var interfaceCIID in host.Interfaces)
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
                                    new ResolvedVariable("LOMADDRESS", "FORCED", @interface.IP ?? ""),
                                    new ResolvedVariable("LOMTYPE", "FORCED", @interface.Name?.ToUpperInvariant() ?? ""),
                                    new ResolvedVariable("LOMNAME", "FORCED", @interface.DnsName ?? ""),
                                });
                                break;
                            }
                        }
                    }
                }

                // set alerting ID to foreign key for special instances
                if (host.Instance == "SERVER-CH")
                {
                    rv.Add(new ResolvedVariable("ALERTCIID", "FORCED", host.ForeignKey ?? ""));
                }
                else
                {
                    rv.Add(new ResolvedVariable("ALERTCIID", "FORCED", host.ID));
                }

                // alerts
                if (host.Status != "ACTIVE" && host.Status != "INFOALERTING")
                { // disable ALERTS for non-active and non-infoalerting
                    rv.Add(new ResolvedVariable("ALERTS", "FORCED", "OFF"));
                }
                else if ((host.Environment == "DEV" || host.Environment == "QM") && (host.AppSupportGroup == argusGroupCIID || host.OSSupportGroup == argusGroupCIID))
                { // disable ALERTS for DEV/QM ARGUS systems
                    rv.Add(new ResolvedVariable("ALERTS", "FORCED", "OFF"));
                }
                else
                {
                    rv.Add(new ResolvedVariable("ALERTS", "FORCED", "ON"));
                }
            }

            // ...from cmdb services
            foreach (var (ciid, service, profile) in servicesWithCategoryProfiles)
            {
                var rv = resolvedVariables[ciid];

                // base
                var customerNickname = "UNKNOWN";
                if (service.Customer.HasValue && customers.TryGetValue(service.Customer.Value, out var customer))
                    customerNickname = customer.Nickname;

                var supportGroupName = "UNKNOWN";
                if (service.SupportGroup.HasValue && groups.TryGetValue(service.SupportGroup.Value, out var supportGroup))
                    supportGroupName = supportGroup.Name;
                rv.AddRange(new List<ResolvedVariable>()
                {
                    new ResolvedVariable("HASNRPE", "FORCED", "YES"),
                    new ResolvedVariable("DYNAMICADD", "FORCED", "NO"),
                    new ResolvedVariable("DYNAMICMODULES", "FORCED", "YES"),

                    new ResolvedVariable("ENVIRONMENT", "FORCED", service.Environment ?? ""),
                    new ResolvedVariable("STATUS", "FORCED", service.Status ?? ""),
                    new ResolvedVariable("CRITICALITY", "FORCED", service.Criticality ?? ""),

                    new ResolvedVariable("CIID", "FORCED", service.ID),
                    new ResolvedVariable("CINAME", "FORCED", service.Name ?? ""),
                    new ResolvedVariable("CONFIGSOURCE", "FORCED", "monmanagement"),

                    new ResolvedVariable("CUST", "FORCED", customerNickname),
                    new ResolvedVariable("CUST_ESCAPED", "FORCED", escapeCustomerNickname(customerNickname)),

                    new ResolvedVariable("INSTANCE", "FORCED", service.Instance ?? ""),
                    new ResolvedVariable("FSOURCE", "FORCED", service.ForeignSource ?? ""),
                    new ResolvedVariable("FKEY", "FORCED", service.ForeignKey ?? ""),

                    new ResolvedVariable("SUPP_OS", "FORCED", supportGroupName),

                    new ResolvedVariable("MONITORINGPROFILE", "FORCED", profile.Name),
                });

                // TODO
                //$resultRef[$id]['VARS']['MONITORINGPROFILE_ORIG'] = join(',', $resultRef[$id]['PROFILE_ORIG']);
                //$resultRef[$id]['VARS']['SUPP_APP'] = $resultRef[$id]['SUPP_APP'];

                // extract oracle db connection string
                if (Regex.IsMatch(service.Class ?? "", "DB") && Regex.IsMatch(service.Type ?? "", "ORACLE"))
                {
                    var foundServiceAction = serviceActionServiceIDLookup[service.ID].FirstOrDefault();
                    if (foundServiceAction != null)
                    {
                        if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                        {
                            rv.AddRange(new List<ResolvedVariable>()
                            {
                                new ResolvedVariable("ORACLECONNECT", "FORCED", (foundServiceAction.Command ?? "").Replace(" ", "")),
                                new ResolvedVariable("ORACLEUSER", "FORCED", foundServiceAction.CommandUser ?? ""),
                            });
                        }
                    }
                }

                // SD-WAN
                if ((service.Class == "APP_ROUTING" || service.Class == "SVC_ROUTING") && Regex.IsMatch(service.Type ?? "", "^SD-WAN.*"))
                {
                    var foundServiceAction = serviceActionServiceIDLookup[service.ID].FirstOrDefault();
                    if (foundServiceAction != null)
                    {
                        if (foundServiceAction.Type.Equals("MONITORING", StringComparison.InvariantCultureIgnoreCase))
                        {
                            rv.AddRange(new List<ResolvedVariable>()
                            {
                                new ResolvedVariable("SDWANCHECKCONFIG", "FORCED", foundServiceAction.Command ?? ""),
                                new ResolvedVariable("SDWANORG", "FORCED", foundServiceAction.CommandUser ?? ""),
                            });
                        }
                    }
                }

                rv.Add(new ResolvedVariable("ALERTCIID", "FORCED", service.ID));

                if (service.Status != "ACTIVE" && service.Status != "INFOALERTING")
                {
                    // disable ALERTS for non-active and non-infoalerting
                    rv.Add(new ResolvedVariable("ALERTS", "FORCED", "OFF"));
                }
                else
                {
                    rv.Add(new ResolvedVariable("ALERTS", "FORCED", "ON"));
                }
            }

            var debugOutput = false; // TODO: remove or make configurable

            // write output
            var variableComparer = new ResolvedVariableComparer();
            var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            foreach (var v in resolvedVariables)
            {
                // merge process for variables
                var groupedByName = v.Value.GroupBy(v => v.Name)
                    .OrderBy(g => g.Key); // order by key (=variable Name) to produce stable output, otherwise JSON changes all the time, leading to "fake" changes

                var debugStr = "";
                var d = new JsonObject();
                foreach (var variablesOfCI in groupedByName)
                {
                    var ordered = variablesOfCI.OrderBy(v => v, variableComparer);

                    var inner = new JsonObject();
                    var first = ordered.First();
                    inner["value"] = first.Value;
                    inner["refType"] = first.OutputRefType;

                    if (debugOutput)
                        debugStr += $"{variablesOfCI.Key}           {first.Value}\n";

                    var chain = new JsonArray();
                    foreach (var vv in ordered.Skip(1))
                        chain.Add(new JsonObject()
                        {
                            ["value"] = vv.Value,
                            ["refType"] = vv.OutputRefType
                        });
                    inner["chain"] = chain;

                    d.Add(variablesOfCI.Key, inner);
                }
                var value = AttributeScalarValueJSON.BuildFromString(d.ToJsonString(), false);
                fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables", value, v.Key));
                if (debugOutput)
                {
                    var valueDebug = new AttributeScalarValueText(debugStr, true);
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables_debug", valueDebug, v.Key));
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
                    "FORCED" => -1,
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

        // internally, we use the "FORCED" refType, but for outputting, we replace it with "CI"
        public string OutputRefType {
            get {
                if (RefType == "FORCED")
                    return "CI";
                return RefType;
            }
        }
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
