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

            // NOTE there are 4 layer defined in config, not sure the meaning of each, for now will use all the data 
            //      from the layer variable_rendering.

            var layersetVariableRendering = await layerModel.BuildLayerSet(new[] { "variable_rendering" }, trans);

            // TODO how to implement input_whitelist and input_blacklist

            var allCIs = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layersetVariableRendering, false, AllAttributeSelection.Instance, trans, changesetProxy.TimeThreshold);

            // TODO NOTE we should create traits dynamically based on configuration, how to do this??
            //var mainCIs = await traitModel.FilterCIsWithTrait(allCIs, Traits.ServicesStaticFlattened, layersetVariableRendering, trans, changesetProxy.TimeThreshold);

            var mainCIs = new List<MergedCI>();

            foreach (var ci in allCIs)
            {
                if (ci.MergedAttributes.ContainsKey(cfg.BaseCI.RequiredTrait))
                {
                    mainCIs.Add(ci);
                }
            }

            // fetch all relations
            var allRelations = await relationModel.GetRelations(RelationSelectionAll.Instance, "variable_rendering", trans, changesetProxy.TimeThreshold);

            // foreach mainCI follow realtions
            foreach (var mainCI in mainCIs)
            {
                // follow realtions
                foreach (var followRelation in cfg.BaseCI.FollowRelations)
                {
                    var previousNode = mainCI;

                    foreach (var follow in followRelation.Follow)
                    {
                        // get the predicate remove the first row
                        var predicate = follow.Predicate[1..];

                        // NOTE map attributes based on attribute maping

                        // TODO NOTE: maybe we need to distinguish between incoming and outgoing relations, have two loops??
                        if (follow.Predicate[0] == '<')
                        {
                            // NOTE does < mean incoming relation?
                            var r = allRelations.Where(r => r.ToCIID == previousNode.ID && r.PredicateID == predicate).FirstOrDefault();

                            if (r != null)
                            {
                                var targetCI = allCIs.Where(ci => ci.ID == r.FromCIID).FirstOrDefault();


                                if (follow.AttributeMapping.Count > 0)
                                {
                                    foreach (var attributeMapping in follow.AttributeMapping)
                                    {
                                        //attributeMapping.Source
                                        // find all attributes that fullfill the condition

                                        var srcAttributes = targetCI.MergedAttributes.Where(attr => attr.Key.StartsWith(attributeMapping.Source[..1]));

                                    }

                                }
                                else
                                {

                                }

                                foreach (var (key, attr) in targetCI.MergedAttributes)
                                {
                                    var a = attr.Attribute.Value.Value2String();

                                    // NOTE use follow attribute mapping if this is null use base ci attribute mapping


                                }
                            }
                        }
                        else if (follow.Predicate[0] == '>')
                        {
                            var r = allRelations.Where(r => r.FromCIID == previousNode.ID && r.PredicateID == predicate).FirstOrDefault();

                            if (r != null)
                            {
                                var targetCI = allCIs.Where(ci => ci.ID == r.ToCIID).FirstOrDefault();

                                if (follow.AttributeMapping.Count > 0)
                                {

                                    foreach (var attributeMapping in follow.AttributeMapping)
                                    {
                                        //attributeMapping.Source
                                        // find all attributes that fullfill the source condition

                                        var srcAttributes = targetCI.MergedAttributes.Where(attr =>
                                        {
                                            if (attributeMapping.Source == "*")
                                            {
                                                return true;
                                            }
                                            else if (attr.Key.StartsWith(attributeMapping.Source[..1]))
                                            {
                                                return true;
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }).ToList();

                                        if (srcAttributes.Count == 0)
                                        {
                                            continue;
                                        }

                                        foreach (var (key, srcAttribute) in srcAttributes)
                                        {
                                            // add this value to the main ci based on target attribute
                                            var attrName = attributeMapping.Target.Replace("{SOURCE}", srcAttribute.Attribute.Name);

                                            // TODO this can trigger some performance issues, the best way would be only to edit the dictionary of this CI
                                            // and add all attribute changes in one call for all CIs.
                                            var (_, changed) = await attributeModel.InsertAttribute(attrName, new AttributeScalarValueText(srcAttribute.Attribute.Value.Value2String()), mainCI.ID, "variable_rendering", changesetProxy, new DataOriginV1(DataOriginType.Manual), trans);

                                            if (!changed)
                                            {
                                                logger.LogError($"An error ocurred trying to insert attribute for CI with id={mainCI.ID}");
                                            }
                                        }
                                    }

                                }
                                else
                                {

                                }

                                previousNode = targetCI;
                            }
                        }

                    }
                }
            }

            return true;
        }
    }
}
