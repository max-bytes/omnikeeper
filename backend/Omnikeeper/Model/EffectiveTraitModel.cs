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
        private readonly IOnlineAccessProxy onlineAccessProxy;
        private readonly ICIModel ciModel;
        private readonly IAttributeModel attributeModel;
        private readonly IRelationModel relationModel;
        private readonly ILogger<EffectiveTraitModel> logger;

        public EffectiveTraitModel(ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IOnlineAccessProxy onlineAccessProxy,
            ILogger<EffectiveTraitModel> logger)
        {
            this.onlineAccessProxy = onlineAccessProxy;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
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
                    resolved.Add(outValue.et);
            }
            return resolved;
        }

        public async Task<IEnumerable<MergedCI>> FilterCIsWithTrait(IEnumerable<MergedCI> cis, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = await CanResolve(trait, cis, layers, trans, atTime);
            return ret;
        }

        public async Task<EffectiveTrait?> GetEffectiveTraitForCI(MergedCI ci, ITrait trait, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var t = await Resolve(trait, new MergedCI[] { ci }, layers, trans, atTime);
            if (t.TryGetValue(ci.ID, out var outValue))
                return outValue.et;
            return null;
        }


        public async Task<IEnumerable<MergedCI>> GetMergedCIsWithTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty && !(trait is TraitEmpty))
                return ImmutableList<MergedCI>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            bool bail;
            (ciidSelection, bail) = await Prefilter(trait, layerSet, ciidSelection, trans, atTime);
            if (bail)
                return ImmutableList<MergedCI>.Empty;

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var fetchEmptyCIs = (trait is TraitEmpty); // only fetch empty traits if the specified trait is empty, otherwise we don't have to do that because a non-empty CI can never fulfill the empty trait
            var cis = await ciModel.GetMergedCIs(ciidSelection, layerSet, fetchEmptyCIs, trans, atTime); 
            var cisWithTrait = await CanResolve(trait, cis, layerSet, trans, atTime);
            return cisWithTrait;
        }

        private async Task<(ICIIDSelection filteredSelection, bool bail)> Prefilter(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            // TODO: this is not even faster for a lot of cases, consider when to actually use this
            var runPrecursorFiltering = false;
            // do a precursor filtering based on required attribute names
            // we can only do this filtering (better performance) when the trait has required attributes AND no online inbound layers are in play
            if (runPrecursorFiltering && trait is GenericTrait tt && tt.RequiredAttributes.Count > 0 && !(await onlineAccessProxy.ContainsOnlineInboundLayer(layerSet, trans)))
            {
                var requiredAttributeNames = tt.RequiredAttributes.Select(a => a.AttributeTemplate.Name);
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

        public async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> GetEffectiveTraitsForTrait(ITrait trait, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (layerSet.IsEmpty && !(trait is TraitEmpty))
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty; // return empty, an empty layer list can never produce any traits (except for the empty trait)

            bool bail;
            (ciidSelection, bail) = await Prefilter(trait, layerSet, ciidSelection, trans, atTime);
            if (bail)
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty;

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            var cis = await ciModel.GetMergedCIs(ciidSelection, layerSet, false, trans, atTime);
            var ets = await Resolve(trait, cis, layerSet, trans, atTime);
            return ets;
        }

        public async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> GetEffectiveTraitsWithTraitAttributeValue(ITrait trait, string traitAttributeIdentifier, IAttributeValue value, LayerSet layerSet, ICIIDSelection ciidSelection, IModelContext trans, TimeThreshold atTime)
        {
            if (trait is TraitEmpty)
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty; // return empty, the empty trait can never have any attributes
            if (layerSet.IsEmpty)
                return ImmutableDictionary<Guid, (MergedCI ci, EffectiveTrait et)>.Empty; // return empty, an empty layer list can never produce any traits

            // extract actual attribute name from trait attributes
            if (!(trait is GenericTrait genericTrait)) throw new Exception("Unknown trait type detected");
            var traitAttribute = genericTrait.RequiredAttributes.FirstOrDefault(a => a.Identifier == traitAttributeIdentifier) ?? genericTrait.OptionalAttributes.FirstOrDefault(a => a.Identifier == traitAttributeIdentifier);
            if (traitAttribute == null) throw new Exception($"Trait Attribute identifier {traitAttribute} does not exist in trait {trait.ID}");

            // get ciids of CIs that contain a fitting attribute (name + value) in ANY of the relevant layers
            // the union of those ciids serves as the ciid selection basis for the next step
            var attributeName = traitAttribute.AttributeTemplate.Name;
            var candidateCIIDs = new HashSet<Guid>();
            foreach (var layerID in layerSet) {
                var ciids = await attributeModel.FindCIIDsWithAttributeNameAndValue(attributeName, value, ciidSelection, layerID, trans, atTime);
                candidateCIIDs.UnionWith(ciids);
            }

            // now do a full pass to check which ci's REALLY fulfill the trait's requirements
            // also check (again) if the final mergedCI fulfills the attribute requirement
            var cis = await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(candidateCIIDs), layerSet, false, trans, atTime);

            var ets = await Resolve(trait, cis, layerSet, trans, atTime);
            return ets.Where(t => {
                if (t.Value.et.TraitAttributes.TryGetValue(traitAttributeIdentifier, out var outValue))
                    if (outValue.Attribute.Value.Equals(value))
                        return true;
                return false;
            }).ToDictionary(t => t.Key, t => t.Value);
        }

        private async Task<IEnumerable<MergedCI>> CanResolve(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
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
                        if (ci.MergedAttributes.IsEmpty()) // TODO: check for relations too?
                            ret.Add(ci);
                    return ret;
                default:
                    throw new Exception("Unknown trait encountered");
            }
        }

        private async Task<IDictionary<Guid, (MergedCI ci, EffectiveTrait et)>> Resolve(ITrait trait, IEnumerable<MergedCI> cis, LayerSet layers, IModelContext trans, TimeThreshold atTime)
        {
            var ret = new Dictionary<Guid, (MergedCI ci, EffectiveTrait et)>(cis.Count());
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
                        ret.Add(ci.ID, (ci, resolvedET));

                        ENDOFCILOOP:
                        ;
                    }

                    return ret;

                case TraitEmpty te:
                    foreach (var ci in cis)
                        if (ci.MergedAttributes.IsEmpty()) // TODO: check for relations too?
                            ret.Add(ci.ID, (ci, new EffectiveTrait(te, new Dictionary<string, MergedCIAttribute>(), new Dictionary<string, IEnumerable<MergedRelation>>(), new Dictionary<string, IEnumerable<MergedRelation>>())));
                    return ret;
                default:
                    throw new Exception("Unknown trait encountered");
            }
        }
    }
}
