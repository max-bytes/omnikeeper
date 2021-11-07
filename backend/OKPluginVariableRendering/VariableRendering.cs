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
        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start VariableRendering");

            //return false;

            Configuration cfg;  

            try
            {
                cfg = config.ToObject<Configuration>();
            }
            catch (Exception ex)
            {
                logger.LogError("An error ocurred while creating configuration instance.", ex);
                return false;
            }

            // TODO NOTE loop through defined layers

            var layersetVariableRendering = await layerModel.BuildLayerSet(new[] { "variable_rendering" }, trans);

            // TODO how to implement input_whitelist and input_blacklist

            var allCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetVariableRendering, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);

            // TODO NOTE we should create traits dynamically based on configuration, how to do this??
            //var mainCIs = await traitModel.FilterCIsWithTrait(allCIs, Traits.ServicesStaticFlattened, layersetVariableRendering, trans, changesetProxy.TimeThreshold);

            var mainCIs = allCIs.Where(ci => ci.MergedAttributes.ContainsKey(cfg.BaseCI.RequiredTrait)).ToList();

            // fetch all relations
            var allRelations = await relationModel.GetRelations(RelationSelectionAll.Instance, "variable_rendering", trans, changesetProxy.TimeThreshold);

            


            // TODO: priority of each element should be taken into account

            foreach (var mainCI in mainCIs)
            {
                var gatheredAttributes = new List<GatheredAttribute>();
                //var gatheredAttributes = new Dictionary<string, (Guid, string)>(); // original name, ci, value

                foreach (var followRelation in cfg.BaseCI.FollowRelations)
                {
                    var prevCI = mainCI;
                    foreach (var follow in followRelation.Follow)
                    {
                        // get the predicate remove the first char, first char defines the direction of relation
                        var predicate = follow.Predicate[1..];

                        if (follow.Predicate[0] == '<')
                        {
                            var b = 9;
                        }

                        // NOTE first check input_blacklist and input_whitelist for this follow

                        // check outgoing relations

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

                        var targetCI = allCIs.Where(ci => ci.ID == r.ToCIID).FirstOrDefault();

                        prevCI = targetCI;

                        var targetCIAttributes = targetCI.MergedAttributes.Where(a => IsAttributeAllowed(a.Value.Attribute.Name, follow.InputWhitelist, follow.InputBlacklist)).ToList();

                        foreach (var mapping in follow.AttributeMapping)
                        {
                            foreach (var (_, attribute) in targetCIAttributes)
                            {
                                if (IsAttributeIncludedInSource(attribute.Attribute.Name, mapping.Source))
                                {
                                    // check 

                                    gatheredAttributes.Add(new GatheredAttribute
                                    {
                                        SourceCIID = targetCI.ID,
                                        OriginalName = attribute.Attribute.Name,
                                        NewName = GetTargetName(attribute.Attribute.Name, mapping.Target),
                                        Value = attribute.Attribute.Value.Value2String(),
                                        RequiredTrait = follow.RequiredTrait,
                                    });
                                }
                            }
                        }
                    }
                }

                // for all gathered attributes insert them to the main ci

                foreach (var attribute in gatheredAttributes)
                {
                    // first we need to check input whitelist and blacklist

                    if (!IsAttributeAllowed(attribute.NewName, cfg.BaseCI.InputWhitelist, cfg.BaseCI.InputBlacklist))
                    {
                        continue;
                    }

                    // check base ci attribute mapping

                    if (!IsAttributeIncludedInSource(attribute.NewName, cfg.BaseCI.AttributeMapping[0].Source))
                    {
                        continue;
                    }

                    var (_, changed) = await attributeModel.InsertAttribute(
                        GetTargetName(attribute.NewName, cfg.BaseCI.AttributeMapping[0].Target),
                        new AttributeScalarValueText(attribute.Value), 
                        mainCI.ID, 
                        "variable_rendering", 
                        changesetProxy, 
                        new DataOriginV1(DataOriginType.Manual), 
                        trans);

                    if (!changed)
                    {
                        logger.LogError($"An error ocurred trying to insert attribute for CI with id={mainCI.ID}");
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
            else if (attribute.StartsWith(source[..1]))
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
            public string OriginalName { get; set; }
            public string RequiredTrait { get; set; }
            public string NewName { get; set; }
            public string Value { get; set; }

            public GatheredAttribute()
            {
                OriginalName = "";
                NewName = "";
                Value = "";
                RequiredTrait = "";
            }
        }
    }
}
