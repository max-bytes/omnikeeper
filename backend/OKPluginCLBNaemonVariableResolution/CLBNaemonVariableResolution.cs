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
                return cfg?.CMDBInputLayerSet.Union(cfg?.MonmanV1InputLayerSet ?? new List<string>()).ToHashSet();
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

            var hostTraitModel = new GenericTraitEntityModel<TargetHost, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var serviceTraitModel = new GenericTraitEntityModel<TargetService, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var naemonV1VariableModel = new GenericTraitEntityModel<NaemonV1Variable, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var customerTraitModel = new GenericTraitEntityModel<Customer, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var profileTraitModel = new GenericTraitEntityModel<Profile, long>(effectiveTraitModel, ciModel, attributeModel, relationModel);
            var categoryTraitModel = new GenericTraitEntityModel<Category, string>(effectiveTraitModel, ciModel, attributeModel, relationModel);

            var hosts = await hostTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var hostID2CIIDLookup = hosts.ToDictionary(h => h.Value.ID, h => h.Key);
            var services = await serviceTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var serviceID2CIIDLookup = services.ToDictionary(h => h.Value.ID, h => h.Key);

            var naemonV1Variables = await naemonV1VariableModel.GetAllByCIID(monmanV1InputLayerset, trans, timeThreshold);

            var customers = await customerTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);
            var customerNicknameLookup = customers.ToDictionary(c => c.Value.Nickname, c => c.Value);

            var profiles = await profileTraitModel.GetAllByDataID(monmanV1InputLayerset, trans, timeThreshold);

            var categories = await categoryTraitModel.GetAllByCIID(cmdbInputLayerset, trans, timeThreshold);

            // TODO: this is not properly defined, because there can be multiple categories with the same name in different trees 
            // TODO: lookup how monman_v1 implements this and how it looks up the proper category from the profile
            // for now, we just pick the first that fits at random
            // also, we uppercase the category names
            var categoryNameLookup = categories.GroupBy(c => c.Value.Name).ToDictionary(t => t.Key.ToUpperInvariant(), t => t.First().Value);

            // collect variables
            var resolvedVariables = new Dictionary<Guid, IList<NaemonV1Variable>>();
            foreach (var ciid in hosts.Keys)
                resolvedVariables.TryAdd(ciid, new List<NaemonV1Variable>());
            foreach (var ciid in services.Keys)
                resolvedVariables.TryAdd(ciid, new List<NaemonV1Variable>());
            foreach (var v in naemonV1Variables)
            {
                var refID = v.Value.refID;
                switch (v.Value.refType)
                {
                    case "CI":
                        {
                            if (hostID2CIIDLookup.TryGetValue(refID, out var refHostCIID))
                            {
                                resolvedVariables[refHostCIID].Add(v.Value);
                            } 
                            else if (serviceID2CIIDLookup.TryGetValue(refID, out var refServiceCIID))
                            {
                                resolvedVariables[refServiceCIID].Add(v.Value);
                            } 
                            else
                            {
                                logger.LogWarning($"Could not find referenced CI with refID \"{refID}\" for variable \"{v.Value.ID}\", skipping variable");
                            }
                        }
                    break;
                    case "GLOBAL":
                        foreach (var rv in resolvedVariables)
                            rv.Value.Add(v.Value);
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
                                foreach(var targetCIID in category.Members)
                                {
                                    if (resolvedVariables.TryGetValue(targetCIID, out var targetList))
                                    {
                                        targetList.Add(v.Value);
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
                            foreach(var targetCIID in foundCustomer.AssociatedCIs)
                            {
                                if (resolvedVariables.TryGetValue(targetCIID, out var targetList))
                                {
                                    targetList.Add(v.Value);
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
                        logger.LogWarning($"Could not process variable \"{v.Value.ID}\": invalid refType \"{v.Value.refType}\"");
                        break;
                }
            }

            // write output
            var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();
            var naemonV1Comparer = new NaemonV1Comparer();
            foreach (var v in resolvedVariables)
            {
                // merge process for variables
                var groupedByName = v.Value.GroupBy(v => v.name)
                    .OrderBy(g => g.Key); // order by key (=variable Name) to produce stable output, otherwise JSON changes all the time, leading to "fake" changes
                var d = new JsonObject();
                foreach(var group in groupedByName)
                {
                    var ordered = group.OrderBy(v => v, naemonV1Comparer);

                    var inner = new JsonObject();
                    var first = ordered.First();
                    inner["value"] = first.value;
                    inner["refType"] = first.refType;

                    var chain = new JsonArray();
                    foreach (var vv in ordered.Skip(1))
                        chain.Add(new JsonObject()
                        {
                            ["value"] = vv.value,
                            ["refType"] = vv.refType
                        });
                    inner["chain"] = chain;

                    d.Add(group.Key, inner);
                }
                var value = AttributeScalarValueJSON.BuildFromString(d.ToJsonString(), false);
                fragments.Add(new BulkCIAttributeDataLayerScope.Fragment("monman_v2.resolved_variables", value, v.Key));
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

        public class NaemonV1Comparer : IComparer<NaemonV1Variable>
        {
            public int Compare(NaemonV1Variable? v1, NaemonV1Variable? v2)
            {
                if (v1 == null && v2 == null) return 0;
                if (v1 == null) return -1;
                if (v2 == null) return 1;
                if (v1.refType == v2.refType)
                {
                    if (v1.precedence > v2.precedence)
                        return -1;
                    else if (v1.precedence < v2.precedence)
                        return 1;
                    else
                    {
                        // we cannot sort "naturally", use the id as the final decider
                        if (v1.ID < v2.ID)
                            return -1;
                        else
                            return 1;
                    }
                } 
                else
                {
                    var refType1 = RefType2Int(v1.refType);
                    var refType2 = RefType2Int(v2.refType);
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
                    "CI" => 0,
                    "PROFILE" => 1,
                    "CUST" => 2,
                    "GLOBAL" => 3,
                    _ => 4, // must not happen, other refTypes should have been filtered out by now
                };
            }
        }
    }
}
