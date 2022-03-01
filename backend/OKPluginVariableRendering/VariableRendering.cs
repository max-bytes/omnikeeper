using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace OKPluginVariableRendering
{
    public class VariableRendering : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly ITraitsProvider traitsProvider;
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ILayerModel layerModel;
        private readonly IAttributeModel attributeModel;
        public VariableRendering(ICIModel ciModel, ILayerModel layerModel, IEffectiveTraitModel effectiveTraitModel,
            IRelationModel relationModel, IAttributeModel attributeModel, ILatestLayerChangeModel latestLayerChangeModel,
            ITraitsProvider traitsProvider) : base(latestLayerChangeModel)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.effectiveTraitModel = effectiveTraitModel;
            this.layerModel = layerModel;
            this.attributeModel = attributeModel;
            this.traitsProvider = traitsProvider;
        }

        const int moduleGroupPrio = 10;
        const int customersGroupPrio = 100;
        const int networkSegmentGroupPrio = 1000;
        const int networkInterfaceGroupPrio = 2000;
        const int assignmentGroupDefaultPrio = 10000;
        const int baseCIPrio = 100000;

        protected override ISet<string> GetDependentLayerIDs(JObject config, ILogger logger)
        {
            try
            {
                var cfg = config.ToObject<Configuration>();
                return cfg.InputLayerSet.ToHashSet();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot parse CLB config");
                return null; // we hit an error parsing the config, cannot extract dependent layers
            }
        }

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start VariableRendering");

            Configuration cfg;

            try
            {
                cfg = config.ToObject<Configuration>();
                logger.LogDebug("Parsed configuration for VariableRendering.");
            }
            catch (Exception ex)
            {
                logger.LogError("An error ocurred while creating configuration instance.", ex);
                return false;
            }

            var layersetVariableRendering = await layerModel.BuildLayerSet(cfg.InputLayerSet.ToArray(), trans);

            var mergedCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetVariableRendering, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);
            var allCIs = mergedCIs.ToDictionary(c => c.ID, c => c);

            var activeTrait = await traitsProvider.GetActiveTrait(cfg.BaseCI.RequiredTrait, trans, changesetProxy.TimeThreshold);

            if (activeTrait == null)
            {
                logger.LogError($"Could not find baseCI required trait {cfg.BaseCI.RequiredTrait}");
                return false;
            }

            var mainCIs = effectiveTraitModel.FilterCIsWithTrait(allCIs.Select(e => e.Value), activeTrait, layersetVariableRendering, trans, changesetProxy.TimeThreshold);
            
            //TODO: select only the realtions that are defined in configuration
            //      check if selection with specific predicates is possible

            var allMergedRelations = await relationModel.GetMergedRelations(RelationSelectionAll.Instance, layersetVariableRendering, trans, changesetProxy.TimeThreshold, MaskHandlingForRetrievalGetMasks.Instance);

            Dictionary<string, List<MergedRelation>> allFromRelations = new();
            Dictionary<string, List<MergedRelation>> allToRelations = new();

            foreach (var r in allMergedRelations)
            {
                var fromKey = $"{r.Relation.PredicateID}-{r.Relation.FromCIID}";
                var toKey = $"{r.Relation.PredicateID}-{r.Relation.ToCIID}";

                if (allFromRelations.ContainsKey(fromKey))
                {
                    allFromRelations[fromKey].Add(r);
                } else
                {
                    allFromRelations.Add(fromKey, new List<MergedRelation> { r });
                }

                if (allToRelations.ContainsKey(toKey))
                {
                    allToRelations[toKey].Add(r);
                }
                else
                {
                    allToRelations.Add(toKey, new List<MergedRelation> { r });
                }
            }

            var fragments = new List<BulkCIAttributeDataLayerScope.Fragment>();

            foreach (var mainCI in mainCIs)
            {

                var gatheredAttributes = new Dictionary<string, GatheredAttribute>();

                foreach (var mapping in cfg.BaseCI.AttributeMapping)
                {
                    foreach (var (name, attribute) in mainCI.MergedAttributes)
                    {
                        if (IsAttributeIncludedInSource(name, mapping.Source))
                        {
                            var a = new GatheredAttribute
                            {
                                Name = GetTargetName(name, mapping.Target),
                                Value = attribute.Attribute.Value,
                                Priority = baseCIPrio,
                            };

                            if (gatheredAttributes.ContainsKey(a.Name))
                            {
                                // check priority of attributes
                                if (gatheredAttributes[a.Name].Priority < a.Priority)
                                {
                                    gatheredAttributes[a.Name] = a;
                                }
                            }
                            else
                            {
                                gatheredAttributes.Add(a.Name, a);
                            }
                        }
                    }
                }

                foreach (var followRelation in cfg.BaseCI.FollowRelations)
                {

                    // get all active traits here

                    var allRequiredTraits = followRelation.Follow.Select(f => f.RequiredTrait);

                    var allActiveTraits = await traitsProvider.GetActiveTraitsByIDs(allRequiredTraits, trans, changesetProxy.TimeThreshold);

                    MergedCI prevCI = mainCI;

                    // save a list with ids for first level of realtions we will need this to check to the second
                    var relationsCIs = new Queue<MergedCI>();
                    relationsCIs.Enqueue(mainCI);

                    foreach (var follow in followRelation.Follow)
                    {

                        var tmpRelationsCIs = new List<MergedCI>();

                        while (relationsCIs.Any())
                        {
                            prevCI = relationsCIs.Dequeue();

                            // get the predicate remove the first char, first char defines the direction of relation
                            var predicate = follow.Predicate[1..];

                            // find CI relations with this predicate
                            List<MergedRelation> mergedRelations = new();

                            if (follow.Predicate[0] == '>')
                            {
                                if (allFromRelations.ContainsKey($"{predicate}-{prevCI.ID}"))
                                {
                                    mergedRelations = allFromRelations[$"{predicate}-{prevCI.ID}"];
                                }
                            }
                            else
                            {
                                if (allFromRelations.ContainsKey($"{predicate}-{prevCI.ID}"))
                                {
                                    mergedRelations = allFromRelations[$"{predicate}-{prevCI.ID}"];
                                }
                            }

                            if (!mergedRelations.Any())
                            {
                                continue;
                            }

                            var targetCIs = new List<MergedCI>();

                            foreach (var r in mergedRelations)
                            {
                                var targetCI = follow.Predicate[0] == '>' ? allCIs[r.Relation.ToCIID] : allCIs[r.Relation.FromCIID];
                                targetCIs.Add(targetCI);
                            }


                            if (!allActiveTraits.TryGetValue(follow.RequiredTrait, out ITrait targetCIRequiredTrait))
                            {
                                continue;
                            }

                            // check the required trait for each CI
                            var filteredCIsWithTrait = effectiveTraitModel.FilterCIsWithTrait(targetCIs, targetCIRequiredTrait, layersetVariableRendering, trans, changesetProxy.TimeThreshold);

                            if (!filteredCIsWithTrait.Any())
                            {
                                continue;
                            }

                            tmpRelationsCIs.AddRange(filteredCIsWithTrait);

                            var gatheredAttrRelationLevel = new Dictionary<string, GatheredAttribute>();

                            foreach (var CI in filteredCIsWithTrait)
                            {
                                var prio = 0;

                                switch (predicate)
                                {
                                    case "belongs_to_customer":
                                        prio = customersGroupPrio;
                                        break;
                                    case "has_network_interface":
                                        prio = networkInterfaceGroupPrio;
                                        break;
                                    case "is_attached_to":
                                        prio = networkSegmentGroupPrio;
                                        break;
                                    case "is_assigned_to":
                                        prio = moduleGroupPrio;
                                        break;
                                    case "belongs_to_assignment_group":
                                        prio = assignmentGroupDefaultPrio;
                                        break;
                                    default:
                                        break;
                                }

                                foreach (var a in CI.MergedAttributes)
                                {
                                    if (!IsAttributeAllowed(a.Value.Attribute.Name, follow.InputWhitelist, follow.InputBlacklist))
                                    {
                                        continue;
                                    }

                                    var attr = new GatheredAttribute
                                    {
                                        Name = a.Value.Attribute.Name,
                                        Value = a.Value.Attribute.Value,
                                        Priority = prio,
                                        CIID = CI.ID,
                                    };

                                    if (gatheredAttrRelationLevel.ContainsKey(attr.Name))
                                    {
                                        // check if current attribute has higher priority
                                        if (gatheredAttrRelationLevel[attr.Name].Priority < prio)
                                        {
                                            gatheredAttrRelationLevel[attr.Name] = attr;
                                        }

                                        // if priorities are the same we shopuld sort based on CI id and take the latest
                                        if (gatheredAttrRelationLevel[attr.Name].Priority == prio)
                                        {
                                            gatheredAttrRelationLevel[attr.Name] = new List<GatheredAttribute> { gatheredAttrRelationLevel[attr.Name], attr }.OrderBy(e => e.CIID).Last();
                                        }
                                    }
                                    else
                                    {
                                        gatheredAttrRelationLevel.Add(attr.Name, attr);
                                    }

                                }
                            }

                            foreach (var mapping in follow.AttributeMapping)
                            {
                                foreach (var (_, attribute) in gatheredAttrRelationLevel)
                                {
                                    if (IsAttributeIncludedInSource(attribute.Name, mapping.Source))
                                    {
                                        attribute.Name = GetTargetName(attribute.Name, mapping.Target);

                                        if (gatheredAttributes.ContainsKey(attribute.Name))
                                        {
                                            if (gatheredAttributes[attribute.Name].Priority > attribute.Priority)
                                            {
                                                gatheredAttributes[attribute.Name] = attribute;
                                            }
                                        }
                                        else
                                        {
                                            gatheredAttributes.Add(attribute.Name, attribute);
                                        }
                                    }
                                }

                            }

                        }

                        tmpRelationsCIs.ForEach(c => relationsCIs.Enqueue(c));
                    }
                }
                
                foreach (var (name, attribute) in gatheredAttributes)
                {
                    fragments.Add(new BulkCIAttributeDataLayerScope.Fragment(name, attribute.Value, mainCI.ID));
                }
            }

            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataLayerScope("", targetLayer.ID, fragments),
                changesetProxy, 
                new DataOriginV1(DataOriginType.ComputeLayer), 
                trans,
                MaskHandlingForRemovalApplyNoMask.Instance,
                OtherLayersValueHandlingForceWrite.Instance);


            return true;
        }

        public static bool IsAttributeAllowed(string attribute, List<string> attributeWhitelist, List<string> attributeBlacklist)
        {
            var result = false;

            if (attributeBlacklist.Count > 0 && attributeBlacklist[0] == "*")
            {
                // NOTE: in this case all attributes are on blacklist
                return result;
            }

            if (attributeWhitelist.Count == 0 || attributeWhitelist[0] == "*")
            {
                // NOTE: in this case all attributes are allowed
                result = true;
            }

            if (attributeWhitelist.Where(a => attribute.StartsWith(a[0..^1])).ToList().Count > 0)
            {
                result = true;
            }


            if (attributeBlacklist.Where(a => attribute.StartsWith(a[0..^1])).ToList().Count > 0)
            {
                result = false;
            }

            return result;
        }

        public static bool IsAttributeIncludedInSource(string attribute, string source)
        {
            var result = false;

            if (source == "*")
            {
                result = true;
            }
            else if (attribute.StartsWith(source[..^1]))
            {
                result = true;
            }

            return result;
        }

        public static string GetTargetName(string source, string target)
        {
            return target.Replace("{SOURCE}", source);
        }

        internal class GatheredAttribute
        {
            public string Name { get; set; }
            public IAttributeValue Value { get; set; }
            public int Priority { get; set; }
            public Guid CIID { get; set; }
            public GatheredAttribute()
            {
                Name = "";
            }
        }
    }
}
