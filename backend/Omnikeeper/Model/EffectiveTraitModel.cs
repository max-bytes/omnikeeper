﻿using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class EffectiveTraitModel : IEffectiveTraitModel
    {
        private readonly ITraitsProvider traitsProvider;
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly ILogger<EffectiveTraitModel> logger;
        public EffectiveTraitModel(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, ITraitsProvider traitsProvider, IOnlineAccessProxy onlineAccessProxy,
            ILogger<EffectiveTraitModel> logger)
        {
            this.traitsProvider = traitsProvider;
            this.onlineAccessProxy = onlineAccessProxy;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.logger = logger;
        }

        public async Task<IEnumerable<EffectiveTrait>> CalculateEffectiveTraitsForCI(MergedCI ci, IModelContext trans, TimeThreshold atTime)
        {
            var traits = (await traitsProvider.GetActiveTraitSet(trans, atTime)).Traits;

            var resolved = new List<EffectiveTrait>();
            foreach (var trait in traits.Values)
            {
                var r = await Resolve(trait, ci, trans, atTime);
                if (r != null)
                    resolved.Add(r);
            }
            return resolved;
        }

        public async Task<bool> DoesCIHaveTrait(MergedCI ci, Trait trait, IModelContext trans, TimeThreshold atTime)
        {
            var ret = await CanResolve(trait, ci, trans, atTime);
            return ret;
        }

        public async Task<EffectiveTrait?> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, IModelContext trans, TimeThreshold atTime)
        {
            return await Resolve(trait, ci, trans, atTime);
        }


        public async Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(Trait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty)
                return ImmutableList<MergedCI>.Empty; // return empty, an empty layer list can never produce any traits

            bool bail;
            (ciidSelection, bail) = await Prefilter(trait, layerSet, ciidSelection, trans, atTime);
            if (bail)
                return ImmutableList<MergedCI>.Empty;

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var cis = await ciModel.GetMergedCIs(ciidSelection, layerSet, false, trans, atTime);
            var ret = new List<MergedCI>();
            foreach (var ci in cis)
            {
                var canResolve = await CanResolve(trait, ci, trans, atTime);
                if (canResolve)
                    ret.Add(ci);
            }

            return ret;
        }

        private async Task<(ICIIDSelection filteredSelection, bool bail)> Prefilter(Trait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            var hasOnlineInboundLayers = false;
            foreach (var l in layerSet)
            {
                if (hasOnlineInboundLayers = await onlineAccessProxy.IsOnlineInboundLayer(l, trans))
                    break;
            }

            // TODO: this is not even faster for a lot of cases, consider when to actually use this
            var runPrecursorFiltering = false;
            // do a precursor filtering based on required attribute names
            // we can only do this filtering (better performance) when the trait has required attributes AND no online inbound layers are in play
            if (runPrecursorFiltering && trait.RequiredAttributes.Count > 0 && !hasOnlineInboundLayers)
            {
                var requiredAttributeNames = trait.RequiredAttributes.Select(a => a.AttributeTemplate.Name);
                ISet<Guid>? candidateCIIDs = null;
                foreach (var requiredAttributeName in requiredAttributeNames)
                {
                    ISet<Guid> ciidsHavingAttributes = new HashSet<Guid>();
                    foreach (var layerID in layerSet.LayerIDs)
                    {
                        var ciids = await attributeModel.FindCIIDsWithAttribute(requiredAttributeName, ciidSelection, layerID, trans, atTime);
                        ciidsHavingAttributes.UnionWith(ciids);
                    }
                    if (candidateCIIDs == null)
                        candidateCIIDs = new HashSet<Guid>(ciidsHavingAttributes);
                    else
                        candidateCIIDs.IntersectWith(ciidsHavingAttributes);
                }
                if (candidateCIIDs!.IsEmpty())
                    return (new AllCIIDsSelection(), true);
                return (SpecificCIIDsSelection.Build(candidateCIIDs!), false);


                // old precursor filter method
                //var requiredAttributeNames = trait.RequiredAttributes.Select(a => a.AttributeTemplate.Name);
                //var lsValues = LayerSet.CreateLayerSetSQLValues(layerSet);
                //using var command = new NpgsqlCommand(@$"
                //    select a.ci_id from
                //    (
                //        select distinct on (inn.name, inn.ci_id) inn.name, inn.ci_id
                //                from(select distinct on(ci_id, name, layer_id) * from
                //                      attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids)
                //                         and name = ANY(@required_attributes)
                //                         order by ci_id, name, layer_id, timestamp DESC NULLS LAST
                //        ) inn
                //        inner join ({lsValues}) as ls(id,""order"") ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
                //        where inn.state != 'removed'::attributestate -- remove entries from layers which' last item is deleted
                //        order by inn.name, inn.ci_id, ls.order DESC
                //    ) a
                //    group by a.ci_id
                //    having count(a.ci_id) = cardinality(@required_attributes)", trans.DBConnection, trans.DBTransaction);
                //command.Parameters.AddWithValue("time_threshold", atTime.Time);
                //command.Parameters.AddWithValue("layer_ids", layerSet.ToArray());
                //command.Parameters.AddWithValue("required_attributes", requiredAttributeNames.ToArray());
                //command.Prepare();
                //using var dr = command.ExecuteReader();

                // T O D O use ciid selection instead of filter
                //var finalCIFilter = ciFilter ?? ((id) => true);

                //var candidateCIIDs = new List<Guid>();
                //while (dr.Read())
                //{
                //    var CIID = dr.GetGuid(0);
                //    if (finalCIFilter(CIID))
                //        candidateCIIDs.Add(CIID);
                //}
                //if (candidateCIIDs.IsEmpty())
                //    return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty;

                //ciidSelection = SpecificCIIDsSelection.Build(candidateCIIDs);
            }
            else
            {
                return (ciidSelection, false); // pass-through
            }
        }

        public async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty)
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty; // return empty, an empty layer list can never produce any traits

            bool bail;
            (ciidSelection, bail) = await Prefilter(trait, layerSet, ciidSelection, trans, atTime);
            if (bail)
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty;

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var cis = await ciModel.GetMergedCIs(ciidSelection, layerSet, false, trans, atTime);
            var ret = new Dictionary<Guid, (MergedCI ci, EffectiveTrait et)>();
            foreach (var ci in cis)
            {
                var et = await Resolve(trait, ci, trans, atTime);
                if (et != null)
                    ret.Add(ci.ID, (ci, et));
            }

            return ret;
        }

        private async Task<bool> CanResolve(Trait trait, MergedCI ci, IModelContext trans, TimeThreshold atTime)
        {
            foreach (var ta in trait.RequiredAttributes)
            {
                var traitAttributeIdentifier = ta.Identifier;
                var (_, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                if (!checks.Errors.IsEmpty())
                    return false;
            };
            if (trait.RequiredRelations.Count > 0)
            {
                var allCompactRelatedCIs = await RelationService.GetCompactRelatedCIs(ci.ID, ci.Layers, ciModel, relationModel, null, trans, atTime);
                foreach (var tr in trait.RequiredRelations)
                {
                    var traitRelationIdentifier = tr.Identifier;
                    var relatedCIs = allCompactRelatedCIs.Where(rci => rci.PredicateID == tr.RelationTemplate.PredicateID);
                    var checks = TemplateCheckService.CalculateTemplateErrorsRelation(relatedCIs, tr.RelationTemplate);
                    if (!checks.Errors.IsEmpty())
                        return false;
                };
            }

            return true;
        }

        private async Task<EffectiveTrait?> Resolve(Trait trait, MergedCI ci, IModelContext trans, TimeThreshold atTime)
        {
            var requiredEffectiveTraitAttributes = trait.RequiredAttributes.Select(ta =>
            {
                var traitAttributeIdentifier = ta.Identifier;
                var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                return (traitAttributeIdentifier, foundAttribute, checks);
            });
            IEnumerable<(string traitRelationIdentifier, IEnumerable<CompactRelatedCI> mergedRelatedCIs, TemplateErrorsRelation checks)> requiredEffectiveTraitRelations
                = new List<(string traitRelationIdentifier, IEnumerable<CompactRelatedCI> mergedRelatedCIs, TemplateErrorsRelation checks)>();
            if (trait.RequiredRelations.Count > 0) // TODO: consider removing requiredRelations... they are TOUGH on performance
            {
                var allCompactRelatedCIs = await RelationService.GetCompactRelatedCIs(ci.ID, ci.Layers, ciModel, relationModel, null, trans, atTime);
                requiredEffectiveTraitRelations = trait.RequiredRelations.Select(tr =>
                {
                    var traitRelationIdentifier = tr.Identifier;
                    var relatedCIs = allCompactRelatedCIs.Where(rci => rci.PredicateID == tr.RelationTemplate.PredicateID);
                    var checks = TemplateCheckService.CalculateTemplateErrorsRelation(relatedCIs, tr.RelationTemplate);
                    return (traitRelationIdentifier, relatedCIs, checks);
                });
            }

            var isTraitApplicable = requiredEffectiveTraitAttributes.All(t => t.checks.Errors.IsEmpty())
                && requiredEffectiveTraitRelations.All(t => t.checks.Errors.IsEmpty());

            if (isTraitApplicable)
            {
                // add optional traitAttributes
                var optionalEffectiveTraitAttributes = trait.OptionalAttributes.Select(ta =>
                {
                    var traitAttributeIdentifier = ta.Identifier;
                    var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                    return (traitAttributeIdentifier, foundAttribute, checks);
                }).Where(t => t.checks.Errors.IsEmpty());

                var resolvedET = new EffectiveTrait(trait,
                    requiredEffectiveTraitAttributes.Concat(optionalEffectiveTraitAttributes).ToDictionary(t => t.traitAttributeIdentifier, t => t.foundAttribute!),
                    requiredEffectiveTraitRelations.ToDictionary(t => t.traitRelationIdentifier, t => t.mergedRelatedCIs));
                return resolvedET;
            }
            else
            {
                return null;
            }
        }
    }
}
