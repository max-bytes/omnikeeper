using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
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
        private readonly IRelationModel relationModel;
        private readonly ILogger<EffectiveTraitModel> logger;

        public EffectiveTraitModel(IRelationModel relationModel, ILogger<EffectiveTraitModel> logger)
        {
            this.relationModel = relationModel;
            this.logger = logger;
        }

        public async Task<IEnumerable<EffectiveTrait>> GetEffectiveTraitsForCI(IEnumerable<ITrait> traits, MergedCI ci, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var resolved = new List<EffectiveTrait>();
            foreach (var trait in traits)
            {
                var r = await Resolve(trait, new MergedCI[] { ci }, layers, trans, atTime);
                if (r.TryGetValue(ci.ID, out var outValue))
                    resolved.Add(outValue);
            }
            return resolved;
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty && !(trait is TraitEmpty))
                return ImmutableList<MergedCI>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            var ret = await CanResolve(trait, cis, layers, trans, atTime);
            return ret;
        }

        public async Task<EffectiveTrait?> GetEffectiveTraitForCI(MergedCI ci, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var t = await Resolve(trait, new MergedCI[] { ci }, layers, trans, atTime);
            if (t.TryGetValue(ci.ID, out var outValue))
                return outValue;
            return null;
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty && !(trait is TraitEmpty))
                return ImmutableDictionary<Guid, EffectiveTrait>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            var ets = await Resolve(trait, cis, layerSet, trans, atTime);
            return ets;
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsWithTraitAttributeValue(ITrait trait, string traitAttributeIdentifier, IAttributeValue value, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            if (trait is TraitEmpty)
                return ImmutableDictionary<Guid, EffectiveTrait>.Empty; // return empty, the empty trait can never have any attributes
            if (layerSet.IsEmpty)
                return ImmutableDictionary<Guid, EffectiveTrait>.Empty; // return empty, an empty layer list can never produce any traits

            // extract actual attribute name from trait attributes
            if (!(trait is GenericTrait genericTrait)) throw new Exception("Unknown trait type detected");
            var traitAttribute = genericTrait.RequiredAttributes.FirstOrDefault(a => a.Identifier == traitAttributeIdentifier) ?? genericTrait.OptionalAttributes.FirstOrDefault(a => a.Identifier == traitAttributeIdentifier);
            if (traitAttribute == null) throw new Exception($"Trait Attribute identifier {traitAttribute} does not exist in trait {trait.ID}");

            // NOTE: we had an alternative implementation that used attributeModel.FindCIIDsWithAttributeNameAndValue() to cut down the number of potential CIs
            // but the implementation for FindCIIDsWithAttributeNameAndValue() was cumbersome and the performance increase was marginal
            // so, to prioritize a simpler API, we removed this implementation and attributeModel.FindCIIDsWithAttributeNameAndValue() with it
            // get ciids of CIs that contain a fitting attribute (name + value) in ANY of the relevant layers
            // the union of those ciids serves as the ciid selection basis for the next step
            //var attributeName = traitAttribute.AttributeTemplate.Name;
            //var candidateCIIDs = new HashSet<Guid>();
            //foreach (var layerID in layerSet) {
            //    var ciids = await attributeModel.FindCIIDsWithAttributeNameAndValue(attributeName, value, ciidSelection, layerID, trans, atTime);
            //    candidateCIIDs.UnionWith(ciids);
            //}
            //// now do a full pass to check which ci's REALLY fulfill the trait's requirements
            //// also check (again) if the final mergedCI fulfills the attribute requirement
            //var cis = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(candidateCIIDs), layerSet, false, trans, atTime);
            //var ets = await Resolve(trait, cis, layerSet, trans, atTime);
            //return ets.Where(t => {
            //    if (t.Value.et.TraitAttributes.TryGetValue(traitAttributeIdentifier, out var outValue))
            //        if (outValue.Attribute.Value.Equals(value))
            //            return true;
            //    return false;
            //}).ToDictionary(t => t.Key, t => t.Value);

            var ets = await GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, atTime);
            return ets.Where(t => {
                if (t.Value.TraitAttributes.TryGetValue(traitAttributeIdentifier, out var outValue))
                    if (outValue.Attribute.Value.Equals(value))
                        return true;
                return false;
            }).ToDictionary(t => t.Key, t => t.Value);
        }

        private async Task<IEnumerable<MergedCI>> CanResolve(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: sanity check: make sure that MergedCIs contain the necessary attributes (in principle), otherwise resolving cannot work properly

            var ret = new List<MergedCI>(cis.Count());
            switch (trait)
            {
                case GenericTrait tt:
                    ILookup<Guid, MergedRelation> fromRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    ILookup<Guid, MergedRelation> toRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    if (tt.RequiredRelations.Count > 0)
                    {
                        var ciids = cis.Select(ci => ci.ID).ToHashSet();
                        if (tt.RequiredRelations.Any(r => r.RelationTemplate.DirectionForward))
                            fromRelations = (await relationModel.GetMergedRelations(RelationSelectionFrom.Build(ciids), layers, trans, atTime)).ToLookup(r => r.Relation.FromCIID);
                        if (tt.RequiredRelations.Any(r => !r.RelationTemplate.DirectionForward))
                            toRelations = (await relationModel.GetMergedRelations(RelationSelectionTo.Build(ciids), layers, trans, atTime)).ToLookup(r => r.Relation.ToCIID);
                    }

                    foreach (var ci in cis)
                    {
                        foreach (var ta in tt.RequiredAttributes)
                        {
                            var (_, errors) = TemplateCheckService.CalculateTemplateErrorsAttributeSimple(ci, ta.AttributeTemplate);
                            if (errors)
                                goto ENDOFCILOOP;
                        };
                        foreach (var tr in tt.RequiredRelations)
                        {
                            var (_, errors) = TemplateCheckService.CalculateTemplateErrorsRelationSimple(fromRelations[ci.ID], toRelations[ci.ID], tr.RelationTemplate);
                            if (errors)
                                goto ENDOFCILOOP;
                        }
                        ret.Add(ci);

                        ENDOFCILOOP:
                        ;
                    }

                    return ret;
                case TraitEmpty te:
                    foreach (var ci in cis)
                        if (ci.MergedAttributes.IsEmpty()) // NOTE: we do not check for relations
                            ret.Add(ci);
                    return ret;
                default:
                    throw new Exception("Unknown trait encountered");
            }
        }

        private async Task<IDictionary<Guid, EffectiveTrait>> Resolve(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: sanity check: make sure that MergedCIs contain the necessary attributes (in principle), otherwise resolving cannot work properly

            var ret = new Dictionary<Guid, EffectiveTrait>(cis.Count());
            switch (trait)
            {
                case GenericTrait tt:
                    ILookup<Guid, MergedRelation> fromRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    ILookup<Guid, MergedRelation> toRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    if (tt.RequiredRelations.Count > 0 || tt.OptionalRelations.Count > 0)
                    {
                        var ciids = cis.Select(ci => ci.ID).ToHashSet();
                        if (tt.RequiredRelations.Any(r => r.RelationTemplate.DirectionForward) || tt.OptionalRelations.Any(r => r.RelationTemplate.DirectionForward))
                            fromRelations = (await relationModel.GetMergedRelations(RelationSelectionFrom.Build(ciids), layers, trans, atTime)).ToLookup(r => r.Relation.FromCIID);
                        if (tt.RequiredRelations.Any(r => !r.RelationTemplate.DirectionForward) || tt.OptionalRelations.Any(r => !r.RelationTemplate.DirectionForward))
                            toRelations = (await relationModel.GetMergedRelations(RelationSelectionTo.Build(ciids), layers, trans, atTime)).ToLookup(r => r.Relation.ToCIID);
                    }

                    foreach (var ci in cis)
                    {
                        var effectiveTraitAttributes = new Dictionary<string, MergedCIAttribute>(tt.RequiredAttributes.Count + tt.OptionalAttributes.Count);
                        var effectiveOutgoingTraitRelations = new Dictionary<string, IEnumerable<MergedRelation>>();
                        var effectiveIncomingTraitRelations = new Dictionary<string, IEnumerable<MergedRelation>>();

                        // required attributes
                        foreach (var ta in tt.RequiredAttributes)
                        {
                            var traitAttributeIdentifier = ta.Identifier;
                            var (foundAttribute, errors) = TemplateCheckService.CalculateTemplateErrorsAttributeSimple(ci, ta.AttributeTemplate);
                            if (errors)
                                goto ENDOFCILOOP;
                            effectiveTraitAttributes.Add(traitAttributeIdentifier, foundAttribute!);
                        }

                        // required relations
                        foreach (var tr in tt.RequiredRelations)
                        {
                            var traitRelationIdentifier = tr.Identifier;
                            var (foundRelations, errors) = TemplateCheckService.CalculateTemplateErrorsRelationSimple(fromRelations[ci.ID], toRelations[ci.ID], tr.RelationTemplate);
                            if (errors)
                                goto ENDOFCILOOP;
                            if (tr.RelationTemplate.DirectionForward)
                                effectiveOutgoingTraitRelations.Add(traitRelationIdentifier, foundRelations!);
                            else
                                effectiveIncomingTraitRelations.Add(traitRelationIdentifier, foundRelations!);
                        }

                        // add optional traitAttributes
                        foreach (var ta in tt.OptionalAttributes)
                        {
                            var traitAttributeIdentifier = ta.Identifier;
                            var (foundAttribute, errors) = TemplateCheckService.CalculateTemplateErrorsAttributeSimple(ci, ta.AttributeTemplate);
                            if (!errors)
                                effectiveTraitAttributes.Add(traitAttributeIdentifier, foundAttribute!);
                        }

                        // add optional traitRelations
                        foreach (var tr in tt.OptionalRelations)
                        {
                            var traitRelationIdentifier = tr.Identifier;
                            var (foundRelations, errors) = TemplateCheckService.CalculateTemplateErrorsRelationSimple(fromRelations[ci.ID], toRelations[ci.ID], tr.RelationTemplate);
                            if (!errors)
                            {
                                if (tr.RelationTemplate.DirectionForward)
                                    effectiveOutgoingTraitRelations.Add(traitRelationIdentifier, foundRelations!);
                                else
                                    effectiveIncomingTraitRelations.Add(traitRelationIdentifier, foundRelations!);
                            }
                        }

                        var resolvedET = new EffectiveTrait(tt, effectiveTraitAttributes, effectiveOutgoingTraitRelations, effectiveIncomingTraitRelations);
                        ret.Add(ci.ID, resolvedET);

                        ENDOFCILOOP:
                        ;
                    }

                    return ret;

                case TraitEmpty te:
                    foreach (var ci in cis)
                        if (ci.MergedAttributes.IsEmpty()) // NOTE: we do not check for relations
                            ret.Add(ci.ID, new EffectiveTrait(te, new Dictionary<string, MergedCIAttribute>(), new Dictionary<string, IEnumerable<MergedRelation>>(), new Dictionary<string, IEnumerable<MergedRelation>>()));
                    return ret;
                default:
                    throw new Exception("Unknown trait encountered");
            }
        }
    }
}
