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

namespace OKPluginVariableRendering
{
    public class VariableRendering : CLBBase
    {
        private readonly IRelationModel relationModel;
        private readonly ICIModel ciModel;
        private readonly IEffectiveTraitModel traitModel;
        private readonly ILayerModel layerModel;
        private readonly IAttributeModel attributeModel;
        public VariableRendering(ICIModel ciModel, ILayerModel layerModel, IEffectiveTraitModel traitModel, IRelationModel relationModel, IAttributeModel attributeModel)
        {
            this.relationModel = relationModel;
            this.ciModel = ciModel;
            this.traitModel = traitModel;
            this.layerModel = layerModel;
            this.attributeModel = attributeModel;
        }

        const int moduleGroupPrio = 10;
        const int customersGroupPrio = 100;
        const int networkSegmentGroupPrio = 1000;
        const int networkInterfaceGroupPrio = 2000;
        const int assignmentGroupDefaultPrio = 10000;
        const int ciPrio = 100000;
         

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
             logger.LogDebug("Start VariableRendering");

            //return false;

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


            foreach (var layer in cfg.InputLayerSet)
            {
                logger.LogDebug($"Started rendering variables for layer {layer}");

                var layersetVariableRendering = await layerModel.BuildLayerSet(new[] { layer }, trans);

                var allCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetVariableRendering, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);

                var mainCIs = allCIs.Where(ci => ci.MergedAttributes.ContainsKey(cfg.BaseCI.RequiredTrait)).ToList();

                // select only the realtions that are defined in configuration
                var relations = new List<string>();

                cfg.BaseCI.FollowRelations.ForEach(r => {
                    r.Follow.ForEach(f =>
                    {
                        if (relations.IndexOf(f.Predicate[1..]) == -1)
                        {
                            relations.Add(f.Predicate[1..]);
                        }
                    });
                });

                // TODO check if selection with specific predicates is possible than use the relations array created earlier
                var allRelations = await relationModel.GetRelations(RelationSelectionAll.Instance, layer, trans, changesetProxy.TimeThreshold);

                // TODO: priority of each element should be taken into account

                foreach (var mainCI in mainCIs)
                {
                    var gatheredAttributes = new List<GatheredAttribute>();

                    foreach (var followRelation in cfg.BaseCI.FollowRelations)
                    {
                        var prevCI = mainCI;
                        foreach (var follow in followRelation.Follow)
                        {
                            // get the predicate remove the first char, first char defines the direction of relation
                            var predicate = follow.Predicate[1..];

                            // check relation direction
                            var r = allRelations.Where(r =>
                            {
                                if (follow.Predicate[0] == '>')
                                {
                                    return r.FromCIID == prevCI.ID && r.PredicateID == predicate;
                                }
                                else
                                {
                                    return r.ToCIID == prevCI.ID && r.PredicateID == predicate;
                                }
                            }).FirstOrDefault();


                            if (r == null)
                            {
                                // NOTE: does this mean that we should break the loop, because there aren't more nodes?? 
                                break;
                            }

                            var targetCI = allCIs.Where(ci => 
                            {
                                if (follow.Predicate[0] == '>')
                                {
                                    return r.ToCIID == ci.ID;
                                }
                                else
                                {
                                    return r.FromCIID == ci.ID;
                                }
                            }
                            ).FirstOrDefault();

                            prevCI = targetCI;

                            var targetCIAttributes = targetCI.MergedAttributes.Where(a => IsAttributeAllowed(a.Value.Attribute.Name, follow.InputWhitelist, follow.InputBlacklist)).ToList();

                            // NOTE check realtion required trait
                            //if (targetCIAttributes.Where(a => a.Key == follow.RequiredTrait).ToList().Count == 0)
                            //{
                            //    continue;
                            //}

                            var allCIAttributes = targetCIAttributes.Select(a => new GatheredAttribute { SourceCIID = targetCI.ID, Name = a.Key, RequiredTrait = follow.RequiredTrait, Value = a.Value.Attribute.Value.ToString() }).ToList();


                            var tmpCIAttributes = new List<GatheredAttribute>();
                            // new way 
                            foreach (var mapping in follow.AttributeMapping)
                            {
                                foreach (var attribute in allCIAttributes)
                                {
                                    if (IsAttributeIncludedInSource(attribute.Name, mapping.Source))
                                    {
                                        var prio = 0;

                                        switch (predicate[1..])
                                        {
                                            case "belongs_to_customer":  prio = customersGroupPrio;
                                                break;
                                            case "has_network_interface": prio = networkInterfaceGroupPrio;
                                                break;
                                            case "is_attached_to": prio = networkSegmentGroupPrio;
                                                break;
                                            case "is_assigned_to": prio = moduleGroupPrio;
                                                break;
                                            case "belongs_to_assignment_group": prio = assignmentGroupDefaultPrio;
                                                break;
                                            default:
                                                break;
                                        }

                                        var a = new GatheredAttribute
                                        {
                                            SourceCIID = targetCI.ID,
                                            Name = GetTargetName(attribute.Name, mapping.Target),
                                            Value = attribute.Value,
                                            RequiredTrait = follow.RequiredTrait,
                                            Priority = prio,
                                        };

                                        var i = tmpCIAttributes.FindIndex(e => e.Name == attribute.Name);

                                        if (i != -1)
                                        {
                                            // check if current attribute has higher priority
                                            if (tmpCIAttributes[i].Priority < a.Priority)
                                            {
                                                tmpCIAttributes.RemoveAt(i);
                                                tmpCIAttributes.Add(a);
                                            }
                                        } else
                                        {
                                            tmpCIAttributes.Add(a);
                                        }

                                        attribute.Name = a.Name;
                                        attribute.Value = a.Value;
                                    }
                                }
                            }

                            gatheredAttributes.AddRange(tmpCIAttributes);
                        }
                    }

                    // for all gathered attributes insert them to the main ci

                    foreach (var mapping in cfg.BaseCI.AttributeMapping)
                    {

                        foreach (var attribute in gatheredAttributes)
                        {
                            // first we need to check input whitelist and blacklist

                            if (!IsAttributeAllowed(attribute.Name, cfg.BaseCI.InputWhitelist, cfg.BaseCI.InputBlacklist))
                            {
                                continue;
                            }

                            // check base ci attribute mapping

                            if (!IsAttributeIncludedInSource(attribute.Name, mapping.Source))
                            {
                                continue;
                            }

                            // check if attribute exists in base ci

                            if (mainCI.MergedAttributes.ContainsKey(attribute.Name) && ciPrio < attribute.Priority)
                            {
                                // don't change this attribute since 
                                continue;
                            }

                            var (_, changed) = await attributeModel.InsertAttribute(
                                GetTargetName(attribute.Name, mapping.Target),
                                new AttributeScalarValueText(attribute.Value),
                                mainCI.ID,
                                layer,
                                changesetProxy,
                                new DataOriginV1(DataOriginType.Manual),
                                trans);

                            if (!changed)
                            {
                                logger.LogError($"An error ocurred trying to insert attribute for CI with id={mainCI.ID}");
                            }
                        }

                    }
                }
            }

            return true;
        }


        private bool IsAttributeAllowed(string attribute, List<string> attributeWhitelist, List<string> attributeBlacklist)
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

            if (attributeWhitelist.Where(a => attribute.StartsWith(a[..1])).ToList().Count > 0)
            {
                result = true;
            }

            if (attributeBlacklist.Where(a => attribute.StartsWith(a[..1])).ToList().Count > 0)
            {
                result = false;
            }

            return result;
        }

        private bool IsAttributeIncludedInSource(string attribute, string source)
        {
            var result = false;

            if (attribute == "__name")
            {
                return result;
            }

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

        private string GetTargetName(string source, string target)
        {
            return target.Replace("{SOURCE}", source);
        }

        internal class GatheredAttribute
        {
            public Guid SourceCIID { get; set; }
            public string RequiredTrait { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }

            public int Priority { get; set; }
            public GatheredAttribute()
            {
                Name = "";
                Value = "";
                RequiredTrait = "";
            }
        }
    }
}
