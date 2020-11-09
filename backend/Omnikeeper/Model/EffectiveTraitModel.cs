﻿using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using Omnikeeper.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Omnikeeper.Base.Inbound;

namespace Omnikeeper.Model
{
    public class EffectiveTraitModel : IEffectiveTraitModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ITraitsProvider traitsProvider;
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly ICIModel ciModel;
        private readonly IRelationModel relationModel;
        private readonly ILogger<EffectiveTraitModel> logger;
        public EffectiveTraitModel(ICIModel ciModel, IRelationModel relationModel, ITraitsProvider traitsProvider, IOnlineAccessProxy onlineAccessProxy, 
            ILogger<EffectiveTraitModel> logger, NpgsqlConnection connection)
        {
            this.traitsProvider = traitsProvider;
            this.onlineAccessProxy = onlineAccessProxy;
            conn = connection;
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.logger = logger;
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetForCIs(IEnumerable<MergedCI> cis, string[] traitNames, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var traits = (await traitsProvider.GetActiveTraitSet(trans, atTime)).Traits;

            var selectedTraits = traits.Where(t => traitNames.Contains(t.Key)).Select(t => t.Value);

            var candidates = cis.SelectMany(ci => selectedTraits.Select(t => new EffectiveTraitCandidate(t, ci))).ToList();
            var ret = await ResolveETCandidates(candidates, trans, atTime);
            return ret;
        }

        public async Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var traits = (await traitsProvider.GetActiveTraitSet(trans, atTime)).Traits;

            var candidates = traits.Values.Select(t => new EffectiveTraitCandidate(t, ci)).ToList();
            var ret = await ResolveETCandidates(candidates, trans, atTime);
            return ret.FirstOrDefault() ?? EffectiveTraitSet.Build(ci, ImmutableList<EffectiveTrait>.Empty);
        }

        public async Task<EffectiveTrait> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ret = await ResolveETCandidates(new List<EffectiveTraitCandidate>() { new EffectiveTraitCandidate(trait, ci) }, trans, atTime);
            return ret.FirstOrDefault()?.EffectiveTraits[trait.Name]; // TODO: this whole thing can be structured better
        }

        public async Task<IEnumerable<MergedCI>> CalculateMergedCIsWithTrait(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null)
        {
            var traits = (await traitsProvider.GetActiveTraitSet(trans, atTime)).Traits;
            var trait = traits.GetValueOrDefault(traitName);
            if (trait == null) return null; // trait not found by name
            return await CalculateMergedCIsWithTrait(trait, layerSet, trans, atTime, ciFilter);
        }
        public async Task<IEnumerable<MergedCI>> CalculateMergedCIsWithTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null)
        {
            var ts = await CalculateEffectiveTraitSetsForTrait(trait, layerSet, trans, atTime, ciFilter);
            return ts.Select(ts => ts.UnderlyingCI);
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> CalculateEffectiveTraitsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null)
        {
            var traits = (await traitsProvider.GetActiveTraitSet(trans, atTime)).Traits;
            var trait = traits.GetValueOrDefault(traitName);
            if (trait == null) return null; // trait not found by name
            return await CalculateEffectiveTraitsForTrait(trait, layerSet, trans, atTime, ciFilter);
        }
        public async Task<IDictionary<Guid, EffectiveTrait>> CalculateEffectiveTraitsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter)
        {
            var ts = await CalculateEffectiveTraitSetsForTrait(trait, layerSet, trans, atTime, ciFilter);
            return ts.ToDictionary(ets => ets.UnderlyingCI.ID, ets => ets.EffectiveTraits[trait.Name]);
        }

        private async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter)
        {
            if (layerSet.IsEmpty)
                return ImmutableList<EffectiveTraitSet>.Empty; // return empty, an empty layer list can never produce any traits

            var hasOnlineInboundLayers = false;
            foreach(var l in layerSet)
            {
                if (hasOnlineInboundLayers = await onlineAccessProxy.IsOnlineInboundLayer(l, trans))
                    break;
            }

            var candidateCIIDs = new List<Guid>();
            // do a precursor filtering based on required attribute names
            // we can only do this filtering (better performance) when the trait has required attributes AND no online inbound layers are in play
            if (trait.RequiredAttributes.Count > 0 && !hasOnlineInboundLayers)
            {
                var requiredAttributeNames = trait.RequiredAttributes.Select(a => a.AttributeTemplate.Name);

                var lsValues = LayerSet.CreateLayerSetSQLValues(layerSet);

                using var command = new NpgsqlCommand(@$"
                    select a.ci_id from
                    (
                        select distinct on (inn.name, inn.ci_id) inn.name, inn.ci_id
                                from(select distinct on(ci_id, name, layer_id) * from
                                      attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids)
                                         and name = ANY(@required_attributes)
                                         order by ci_id, name, layer_id, timestamp DESC
                        ) inn
                        inner join ({lsValues}) as ls(id,""order"") ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
                        where inn.state != 'removed'::attributestate -- remove entries from layers which' last item is deleted
                        order by inn.name, inn.ci_id, ls.order DESC
                    ) a
                    group by a.ci_id
                    having count(a.ci_id) = cardinality(@required_attributes)", conn, trans);
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layerSet.ToArray());
                command.Parameters.AddWithValue("required_attributes", requiredAttributeNames.ToArray());
                using var dr = command.ExecuteReader();

                var finalCIFilter = ciFilter ?? ((id) => true);

                while (dr.Read())
                {
                    var CIID = dr.GetGuid(0);
                    if (finalCIFilter(CIID))
                        candidateCIIDs.Add(CIID);
                }
            }
            else
            {
                var tmp = await ciModel.GetCIIDs(trans);
                if (ciFilter != null)
                    candidateCIIDs.AddRange(tmp.Where(c => ciFilter(c)));
                else
                    candidateCIIDs.AddRange(tmp);
            }

            if (candidateCIIDs.IsEmpty())
                return ImmutableList<EffectiveTraitSet>.Empty;

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var cis = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(candidateCIIDs), layerSet, false, trans, atTime);

            var candidates = cis.Select(ci => new EffectiveTraitCandidate(trait, ci)).ToList();
            var ts = await ResolveETCandidates(candidates, trans, atTime);
            return ts;
        }

        private async Task<IEnumerable<EffectiveTraitSet>> ResolveETCandidates(IEnumerable<EffectiveTraitCandidate> candidates, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var resolved = new List<(EffectiveTraitCandidate candidate, EffectiveTrait result)>();
            foreach (var c in candidates)
            {
                var r = await Resolve(c, trans, atTime);
                if (r != null)
                    resolved.Add((c, r));
            }

            return resolved.GroupBy(c => c.candidate.CI).Select(t => EffectiveTraitSet.Build(t.Key, t.Select(t => t.result)));
        }

        private async Task<EffectiveTrait> Resolve(EffectiveTraitCandidate et, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var ci = et.CI;
            var trait = et.Trait;

            var requiredEffectiveTraitAttributes = trait.RequiredAttributes.Select(ta =>
            {
                var traitAttributeIdentifier = ta.Identifier;
                var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                return (traitAttributeIdentifier, foundAttribute, checks);
            });
            IEnumerable<(string traitRelationIdentifier, IEnumerable<MergedRelatedCI> mergedRelatedCIs, TemplateErrorsRelation checks)> requiredEffectiveTraitRelations
                = new List<(string traitRelationIdentifier, IEnumerable<MergedRelatedCI> mergedRelatedCIs, TemplateErrorsRelation checks)>();
            if (trait.RequiredRelations.Count > 0)
            {
                var allMergedRelatedCIs = (await RelationService.GetMergedRelatedCIs(ci.ID, ci.Layers, ciModel, relationModel, trans, atTime));
                requiredEffectiveTraitRelations = trait.RequiredRelations.Select(tr =>
                {
                    var traitRelationIdentifier = tr.Identifier;
                    var mergedRelatedCIs = allMergedRelatedCIs[tr.RelationTemplate.PredicateID];
                    var checks = TemplateCheckService.CalculateTemplateErrorsRelation(mergedRelatedCIs, tr.RelationTemplate);
                    return (traitRelationIdentifier, mergedRelatedCIs, checks);
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

                var resolvedET = EffectiveTrait.Build(trait,
                    requiredEffectiveTraitAttributes.Concat(optionalEffectiveTraitAttributes).ToDictionary(t => t.traitAttributeIdentifier, t => t.foundAttribute),
                    requiredEffectiveTraitRelations.ToDictionary(t => t.traitRelationIdentifier, t => t.mergedRelatedCIs));
                return resolvedET;
            }
            else
            {
                return null;
            }
        }

        private class EffectiveTraitCandidate
        {
            public EffectiveTraitCandidate(Trait trait, MergedCI ci)
            {
                Trait = trait;
                CI = ci;
            }

            public Trait Trait { get; }
            public MergedCI CI { get; }
        }
    }
}
