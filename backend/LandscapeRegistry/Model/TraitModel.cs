using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using LandscapeRegistry.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitModel : ITraitModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ITraitsProvider traitsProvider;
        private readonly ICIModel ciModel;
        private readonly IRelationModel relationModel;
        private readonly ILogger<TraitModel> logger;
        public TraitModel(ICIModel ciModel, IRelationModel relationModel, ITraitsProvider traitsProvider, ILogger<TraitModel> logger, NpgsqlConnection connection)
        {
            this.traitsProvider = traitsProvider;
            conn = connection;
            this.ciModel = ciModel;
            this.relationModel = relationModel;
            this.logger = logger;
        }

        public async Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var traits = traitsProvider.GetTraits();

            var candidates = traits.Values.Select(t => new EffectiveTraitCandidate(t, ci)).ToList();
            var ret = await ResolveETCandidates(candidates, traits, trans, atTime);
            return ret.FirstOrDefault() ?? EffectiveTraitSet.Build(ci, ImmutableList<EffectiveTrait>.Empty);
        }


        public async Task<EffectiveTrait> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var traits = traitsProvider.GetTraits();
            var ret = await ResolveETCandidates(new List<EffectiveTraitCandidate>() { new EffectiveTraitCandidate(trait, ci) }, traits, trans, atTime);
            return ret.FirstOrDefault()?.EffectiveTraits[trait.Name]; // TODO: this whole thing can be structured better
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            var traits = traitsProvider.GetTraits();
            var trait = traits.GetValueOrDefault(traitName);
            if (trait == null) return null; // trait not found by name
            return await CalculateEffectiveTraitSetsForTrait(trait, layerSet, trans, atTime);
        }

        private async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // do a precursor filtering based on required attribute names
            var requiredAttributeNames = trait.RequiredAttributes.Select(a => a.AttributeTemplate.Name);
            var candidateCIIDs = new List<Guid>();
            var tempLayersetTableName = await LayerSet.CreateLayerSetTempTable(layerSet, "temp_layerset", conn, trans);

            // TODO: consider case with no required attributes, like when a trait only has dependent traits

            using (var command = new NpgsqlCommand(@$"
                select a.ci_id from
                (
                    select distinct on (inn.name, inn.ci_id) inn.name, inn.ci_id
                            from(select distinct on(ci_id, name, layer_id) * from
                                  attribute where timestamp <= @time_threshold and layer_id = ANY(@layer_ids)
                                     and name = ANY(@required_attributes)
                                     order by ci_id, name, layer_id, timestamp DESC
                    ) inn
                    inner join {tempLayersetTableName} ls ON inn.layer_id = ls.id -- inner join to only keep rows that are in the selected layers
                    where inn.state != 'removed'::attributestate -- remove entries from layers which' last item is deleted
                    order by inn.name, inn.ci_id, ls.order DESC
                ) a
                group by a.ci_id
                having count(a.ci_id) = cardinality(@required_attributes)", conn, trans))
            {
                command.Parameters.AddWithValue("time_threshold", atTime.Time);
                command.Parameters.AddWithValue("layer_ids", layerSet.ToArray());
                command.Parameters.AddWithValue("required_attributes", requiredAttributeNames.ToArray());
                using var dr = command.ExecuteReader();

                while (dr.Read())
                {
                    var CIID = dr.GetGuid(0);
                    candidateCIIDs.Add(CIID);
                }
            }

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            // TODO: performance improvement: use a function that works on a list of ci's, not check every ci on its own (and do N queries)
            var cis = await ciModel.GetMergedCIs(layerSet, false, trans, atTime, candidateCIIDs);

            // TODO: check that if the current trait has depedent traits that they are properly resolved too
            var candidates = cis.Select(ci => new EffectiveTraitCandidate(trait, ci)).ToList();
            var traits = traitsProvider.GetTraits();
            var ret = await ResolveETCandidates(candidates, traits, trans, atTime);
            return ret;
        }

        private async Task<IEnumerable<EffectiveTraitSet>> ResolveETCandidates(IEnumerable<EffectiveTraitCandidate> candidates, 
            IImmutableDictionary<string, Trait> traits, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // HACK: this is probably a pathetic, iterative implementation of a dependency graph resolver; it works, but is probably not optimal
            // consider researching for a better implementation
            // maybe implement https://www.electricmonk.nl/docs/dependency_resolving_algorithm/dependency_resolving_algorithm.html, which is a recursive algorithm

            // TODO: or even better: consider resolving dependent traits once at definition time, create resulting, flat traits
            var requiresAdditionalPass = false;
            var loopDetected = false;
            var unresolvedCandidates = new List<EffectiveTraitCandidate>(candidates);
            var lookup = candidates.ToDictionary(c => (c.CI.ID, c.Trait.Name), c => c);
            do
            {
                requiresAdditionalPass = false;
                var addedNewCandidates = false;
                var numResolved = unresolvedCandidates.Count;
                for (int i = 0;i < unresolvedCandidates.Count;i++)
                {
                    var candidate = unresolvedCandidates[i];
                    var newCandidates = await TryToResolve(candidate, traits, lookup, trans, atTime);
                    if (newCandidates != null && newCandidates.Count() > 0)
                    {
                        addedNewCandidates = true;
                        unresolvedCandidates.AddRange(newCandidates);
                        foreach(var nc in newCandidates) lookup.Add((nc.CI.ID, nc.Trait.Name), nc);
                    }
                    if (candidate.IsResolved)
                    {
                        unresolvedCandidates.RemoveAt(i--);
                    }
                    else
                        requiresAdditionalPass = true;
                }

                // check for endless loop -> loop in dependency tree
                if (requiresAdditionalPass && unresolvedCandidates.Count >= numResolved && !addedNewCandidates)
                { // loop detected!
                    loopDetected = true;
                }
            } while (requiresAdditionalPass && !loopDetected);

            if (loopDetected)
            { // in case of loop, resolve all unresolveds
                foreach (var uc in unresolvedCandidates) uc.SetResolved(null);
            }
            // from here on, assumes every candidate is resolved

            return candidates.Where(c => c.ET != null).GroupBy(c => c.CI).Select(t => EffectiveTraitSet.Build(t.Key, t.Select(t => t.ET)));
        }

        private async Task<IEnumerable<EffectiveTraitCandidate>> TryToResolve(EffectiveTraitCandidate et, IImmutableDictionary<string, Trait> traits,
            IDictionary<(Guid ciid, string traitName), EffectiveTraitCandidate> all, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (et.IsResolved) throw new Exception("Trying to resolve an already resolved ETCandidate");

            var ci = et.CI;
            var trait = et.Trait;

            // check for dependent traits, only continue here if all dependent traits are in resolved
            var notFoundTraits = new List<EffectiveTraitCandidate>();
            var requiredTraits = new List<EffectiveTraitCandidate>();
            foreach(var rtName in trait.RequiredTraits)
            {
                all.TryGetValue((ci.ID, rtName), out var rt);
                if (rt == null)
                {
                    if (!traits.TryGetValue(rtName, out var missingTrait))
                    {
                        logger.LogWarning($"Could not resolve dependent trait with name {rtName}");
                    }
                    notFoundTraits.Add(new EffectiveTraitCandidate(missingTrait, ci));
                } else
                {
                    requiredTraits.Add(rt);
                }
            }
            if (notFoundTraits.Count > 0)
            { // not all required traits are found, add them to the list of traits to resolve
                return notFoundTraits;
            }
            var hasUnresolvedRequiredTraits = requiredTraits.Any(rt => !rt.IsResolved);
            if (hasUnresolvedRequiredTraits)
            { // not all required traits are resolved yet, defer resolving of this trait
                return null;
            }
            if (requiredTraits.Any(rt => rt.ET == null))
            { // there exist required traits that are NOT fulfilled -> this trait cannot be fulfilled either, resolve and return
                et.SetResolved(null);
                return null;
            }

            // assume dependent traits are resolved
            var resolvedDependentTraitNames = requiredTraits.Select(rt => rt.Trait.Name).ToList();

            var requiredEffectiveTraitAttributes = trait.RequiredAttributes.Select(ta =>
            {
                var traitAttributeIdentifier = ta.Identifier;
                var (foundAttribute, checks) = TemplateCheckService.CalculateTemplateErrorsAttribute(ci, ta.AttributeTemplate);
                return (traitAttributeIdentifier, foundAttribute, checks);
            });
            IEnumerable<(string traitRelationIdentifier, IEnumerable<(Relation relation, MergedCI toCI)> foundRelations, TemplateErrorsRelation checks)> requiredEffectiveTraitRelations 
                = new List<(string traitRelationIdentifier, IEnumerable<(Relation relation, MergedCI toCI)> foundRelations, TemplateErrorsRelation checks)>();
            if (trait.RequiredRelations.Count > 0)
            {
                var relationsAndToCIs = (await RelationService.GetMergedForwardRelationsAndToCIs(ci.ID, ci.Layers, ciModel, relationModel, trans, atTime))
                    .ToLookup(t => t.relation.PredicateID);
                requiredEffectiveTraitRelations = trait.RequiredRelations.Select(tr =>
                {
                    var traitRelationIdentifier = tr.Identifier;
                    var foundRelations = relationsAndToCIs[tr.RelationTemplate.PredicateID];
                    var checks = TemplateCheckService.CalculateTemplateErrorsRelation(foundRelations, tr.RelationTemplate);
                    return (traitRelationIdentifier, foundRelations, checks);
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
                    requiredEffectiveTraitRelations.ToDictionary(t => t.traitRelationIdentifier, t => t.foundRelations),
                    resolvedDependentTraitNames);
                et.SetResolved(resolvedET);
            } else
            {
                et.SetResolved(null);
            }
            return null;
        }

        private class EffectiveTraitCandidate
        {
            public EffectiveTraitCandidate(Trait trait, MergedCI ci)
            {
                ET = null;
                IsResolved = false;
                Trait = trait;
                CI = ci;
            }

            public Trait Trait { get; }
            public MergedCI CI { get; }

            public EffectiveTrait ET { get; private set; }
            public bool IsResolved { get; private set; }
            public void SetResolved(EffectiveTrait et)
            {
                IsResolved = true;
                ET = et;
            }
        }
    }
}
