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

        public EffectiveTraitModel(IRelationModel relationModel)
        {
            this.relationModel = relationModel;
        }

        /// <summary>
        /// traitSOP is a sum of products of trait requirements
        /// see https://en.wikipedia.org/wiki/Disjunctive_normal_form
        /// </summary>
        public async Task<IEnumerable<MergedCI>> FilterCIsWithTraitSOP(IEnumerable<MergedCI> cis, (ITrait trait, bool negated)[][] traitSOP, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // return the full list if the traitSOP is empty
            if (traitSOP.IsEmpty())
                return cis;

            var ret = new HashSet<MergedCI>();
            for(var i = 0;i < traitSOP.Length;i++)
            {
                var traitP = traitSOP[i];

                IEnumerable<MergedCI>? productResult = null;

                for(var j = 0;j < traitP.Length;j++)
                {
                    var (trait, negated) = traitP[j];
                    var (has, hasNot) = await CanResolve(trait, productResult ?? cis, layers, trans, atTime);
                    if (negated)
                        productResult = hasNot;
                    else
                        productResult = has;
                }

                if (productResult == null)
                    throw new Exception("Invalid traitSOP: array of trait products must not be empty");

                ret.UnionWith(productResult);
            }

            return ret;
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            if (layers.IsEmpty && !(trait is TraitEmpty))
                return ImmutableList<MergedCI>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            var (has, _) = await CanResolve(trait, cis, layers, trans, atTime);
            return has;
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithoutTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var (_, hasNot) = await CanResolve(trait, cis, layers, trans, atTime);
            return hasNot;
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetEffectiveTraitsForTrait(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layerSet, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty && !(trait is TraitEmpty))
                return ImmutableDictionary<Guid, EffectiveTrait>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            var ets = await Resolve(trait, cis, layerSet, trans, atTime);
            return ets;
        }

        private async Task<(IEnumerable<MergedCI> has, IEnumerable<MergedCI> hasNot)> CanResolve(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: sanity check: make sure that MergedCIs contain the necessary attributes (in principle), otherwise resolving cannot work properly

            var has = new List<MergedCI>(cis.Count());
            var hasNot = new List<MergedCI>(cis.Count());
            switch (trait)
            {
                case GenericTrait tt:
                    ILookup<Guid, MergedRelation> fromRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    ILookup<Guid, MergedRelation> toRelations = Enumerable.Empty<MergedRelation>().ToLookup(x => default(Guid));
                    if (tt.RequiredRelations.Count > 0)
                    {
                        var ciids = cis.Select(ci => ci.ID).ToHashSet();
                        // TODO: only fetch relations with relevant predicateIDs
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
                            {
                                hasNot.Add(ci);
                                goto ENDOFCILOOP;
                            }
                        };
                        foreach (var tr in tt.RequiredRelations)
                        {
                            var (_, errors) = TemplateCheckService.CalculateTemplateErrorsRelationSimple(fromRelations[ci.ID], toRelations[ci.ID], tr.RelationTemplate);
                            if (errors)
                            {
                                hasNot.Add(ci);
                                goto ENDOFCILOOP;
                            }
                        }

                        has.Add(ci);

                        ENDOFCILOOP:
                        ;
                    }
                    return (has, hasNot);
                case TraitEmpty te:
                    foreach (var ci in cis)
                        if (ci.MergedAttributes.IsEmpty()) // NOTE: we do not check for relations
                            has.Add(ci);
                        else
                            hasNot.Add(ci);
                    return (has, hasNot);
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

                        var resolvedET = new EffectiveTrait(ci.ID, tt, effectiveTraitAttributes, effectiveOutgoingTraitRelations, effectiveIncomingTraitRelations);
                        ret.Add(ci.ID, resolvedET);

                        ENDOFCILOOP:
                        ;
                    }

                    return ret;

                case TraitEmpty te:
                    foreach (var ci in cis)
                        if (ci.MergedAttributes.IsEmpty()) // NOTE: we do not check for relations
                            ret.Add(ci.ID, new EffectiveTrait(ci.ID, te, new Dictionary<string, MergedCIAttribute>(), new Dictionary<string, IEnumerable<MergedRelation>>(), new Dictionary<string, IEnumerable<MergedRelation>>()));
                    return ret;
                default:
                    throw new Exception("Unknown trait encountered");
            }
        }
    }
}
