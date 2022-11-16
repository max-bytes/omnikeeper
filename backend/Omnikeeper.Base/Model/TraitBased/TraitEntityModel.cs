using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class TraitEntityModel
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IAttributeModel attributeModel;
        protected readonly IRelationModel relationModel;
        private readonly IChangesetModel changesetModel;
        private readonly ITrait trait;
        private readonly IReadOnlySet<string> relevantAttributesForTrait;
        private readonly IPredicateSelection relevantPredicatesForTrait;

        public TraitEntityModel(ITrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel, IChangesetModel changesetModel)
        {
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.changesetModel = changesetModel;
            this.trait = trait;

            relevantAttributesForTrait = trait.GetRelevantAttributeNames();
            relevantPredicatesForTrait = PredicateSelectionSpecific.Build(trait.GetRelevantPredicateIDs());
        }

        public string TraitID => trait.ID;

        private IOtherLayersValueHandling GetOtherLayersValueHandling(LayerSet readLayerSet, string writeLayerID)
        {
            return OtherLayersValueHandlingTakeIntoAccount.Build(readLayerSet, writeLayerID);
        }

        public async Task<EffectiveTrait?> GetSingleByCIID(Guid ciid, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ci = (await ciModel.GetMergedCIs(SpecificCIIDsSelection.Build(ciid), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold)).FirstOrDefault();
            if (ci == null) return null;
            var r = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, new MergedCI[] { ci }, layerSet, trans, timeThreshold);
            if (r.TryGetValue(ci.ID, out var outValue))
                return outValue;
            return null;
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetByCIID(ICIIDSelection ciidSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(ciidSelection, layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            return cisWithTrait;
        }

        // returns the latest relevant changeset that affects/contributes to any of the trait entities (filtered by ciSelection) at that time
        // NOTE: this is NOT intelligent enough to not return changesets that have no practical effect because their changes are hidden by data in upper layers
        // NOTE: this is NOT intelligent enough to not return changesets that have no practical effect because their changes affect no actual trait entity, but contain changes to CIs
        // where an attribute/relation that (by coincidence) has the same name as one of the trait attributes/relations changed; that's why it has the *Heuristic suffix
        public async Task<Changeset?> GetLatestRelevantChangesetOverallHeuristic(ICIIDSelection ciSelection, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await changesetModel.GetLatestChangesetOverall(ciSelection, NamedAttributesSelection.Build(relevantAttributesForTrait), relevantPredicatesForTrait, layerSet.LayerIDs, trans, timeThreshold);
        }

        // returns the latest relevant changeset PER CI that affects/contributes the trait entity (filtered by ciSelection) at that time
        // NOTE: this is NOT intelligent enough to not return changesets that have no practical effect because their changes are hidden by data in upper layers
        // param filterOutNonTraitEntityCIs: set to true to force sanity checks per CI to ensure they actually have the trait, set to false if you are sure that the passed ciSelection is 100% trait entities (otherwise you get wrong results)
        public async Task<IDictionary<Guid, Changeset>> GetLatestRelevantChangesetPerTraitEntity(ICIIDSelection ciSelection, bool includeRemovedTraitEntities, bool filterOutNonTraitEntityCIs, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (includeRemovedTraitEntities)
                throw new NotImplementedException(); // hard to implement, not supported (yet)
            // possible idea for implementing case when includingRemoved = true
            // GetLatestChangesetPerCI()
            // check cis if they fulfill trait at current time: yes -> add to return set
            // for those who don't, repeat operation with timestamp that is just before the found changeset per CI

            var r = await changesetModel.GetLatestChangesetPerCI(ciSelection, NamedAttributesSelection.Build(relevantAttributesForTrait), relevantPredicatesForTrait, layerSet.LayerIDs, trans, timeThreshold);
            if (filterOutNonTraitEntityCIs)
            {
                var ret = new Dictionary<Guid, Changeset>();
                var ciidSelectionWithChangeset = SpecificCIIDsSelection.Build(r.Keys.ToHashSet());
                var cis = await ciModel.GetMergedCIs(ciidSelectionWithChangeset, layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
                var ciidsWithET = effectiveTraitModel.FilterCIsWithTrait(cis, trait, layerSet).Select(ci => ci.ID).ToHashSet();

                foreach (var ciidWithET in ciidsWithET)
                    if (r.TryGetValue(ciidWithET, out var cs))
                        ret[ciidWithET] = cs;

                return ret;
            } else
            {
                return r;
            }
        }

        /*
         * NOTE: unlike the regular update, this does not do any checks if the updated entities actually fulfill the trait requirements 
         * and will be considered as this trait's entities going forward
         */
        // NOTE: the cis MUST exist already
        public async Task<bool> BulkReplace(ICIIDSelection relevantCIs, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (attributeFragments.IsEmpty() && relevantCIs is NoCIIDsSelection && outgoingRelations.IsEmpty() && incomingRelations.IsEmpty())
                return false;

            var changed = await WriteAttributes(attributeFragments, relevantCIs, NamedAttributesSelection.Build(relevantAttributesForTrait), layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);

            if (!trait.OptionalRelations.IsEmpty())
            {
                var ciids = await relevantCIs.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans)); // TODO: couldn't we stay in CIIDSelection space? Do we really need to materialize?
                var relevantOutgoingRelations = trait.OptionalRelations.Where(rr => rr.RelationTemplate.DirectionForward).SelectMany(rr => ciids.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();
                if (!relevantOutgoingRelations.IsEmpty())
                {
                    var outgoingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true);
                    var tmpChanged = await WriteRelations(outgoingScope, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);
                    changed = changed || tmpChanged;
                }

                var relevantIncomingRelations = trait.OptionalRelations.Where(rr => !rr.RelationTemplate.DirectionForward).SelectMany(rr => ciids.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();
                if (!relevantIncomingRelations.IsEmpty())
                {
                    var incomingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false);
                    var tmpChanged = await WriteRelations(incomingScope, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);
                    changed = changed || tmpChanged;
                }
            }

            return changed;
        }

        // NOTE: the ci MUST exist already
        public async Task<(EffectiveTrait et, bool changed)> InsertOrUpdate(Guid ciid, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            ISet<string>? relevantAttributes, // null if all are relevant
            ISet<string>? relevantOutgoingPredicateIDs, ISet<string>? relevantIncomingPredicateIDs, // null if all are relevant
            string? ciName, LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var finalRelevantAttributes = relevantAttributesForTrait;
            if (relevantAttributes != null)
                finalRelevantAttributes = finalRelevantAttributes.Intersect(relevantAttributes).ToHashSet();

            if (ciName != null)
            {
                finalRelevantAttributes = finalRelevantAttributes.Concat(ICIModel.NameAttribute).ToHashSet();
                attributeFragments = attributeFragments.Concat(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, ICIModel.NameAttribute, new AttributeScalarValueText(ciName)));
            }

            var changed = await WriteAttributes(attributeFragments, SpecificCIIDsSelection.Build(ciid), NamedAttributesSelection.Build(finalRelevantAttributes), layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);

            IEnumerable<TraitRelation> relevantTraitRelations = trait.OptionalRelations;
            if (relevantIncomingPredicateIDs != null)
                relevantTraitRelations = relevantTraitRelations.Where(tr => tr.RelationTemplate.DirectionForward || relevantIncomingPredicateIDs.Contains(tr.RelationTemplate.PredicateID));
            if (relevantOutgoingPredicateIDs != null)
                relevantTraitRelations = relevantTraitRelations.Where(tr => !tr.RelationTemplate.DirectionForward || relevantOutgoingPredicateIDs.Contains(tr.RelationTemplate.PredicateID));

            if (!relevantTraitRelations.IsEmpty())
            {
                var relevantOutgoingRelations = relevantTraitRelations.Where(rr => rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var outgoingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true);
                var tmpChanged = await WriteRelations(outgoingScope, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;

                var relevantIncomingRelations = relevantTraitRelations.Where(rr => !rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var incomingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false);
                tmpChanged = await WriteRelations(incomingScope, layerSet, writeLayer, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;
            }

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("DC does not conform to trait requirements");
            return (dc, changed);
        }

        private async Task<bool> WriteAttributes(IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> fragments,
            ICIIDSelection relevantCIs, IAttributeSelection relevantAttributes, LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans,
            IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
            var numChanged = await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataCIAndAttributeNameScope(writeLayer, fragments, relevantCIs, relevantAttributes), changesetProxy, trans, maskHandlingForRemoval, otherLayersValueHandling);

            return numChanged > 0;
        }

        private async Task<bool> WriteRelations<F>(IBulkRelationData<F> scope, LayerSet layerSet, string writeLayer, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
            var tmpNumChanged = await relationModel.BulkReplaceRelations(scope, changesetProxy, trans, maskHandlingForRemoval, otherLayersValueHandling);
            return tmpNumChanged > 0;
        }

        // NOTE: assumes that the ciids exist, does not check beforehand if trait entities are actually present
        // NOTE: also deletes the __name ci attribute, if it exists
        public async Task<bool> TryToDelete(ICIIDSelection ciSelection, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            await RemoveAllAttributes(ciSelection, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);
            await RemoveAllRelations(ciSelection, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);

            var dcAfterDeletion = await GetByCIID(ciSelection, layerSet, trans, changesetProxy.TimeThreshold);
            return dcAfterDeletion.Count == 0; // return successful if dcs do not exist anymore afterwards
        }

        private async Task RemoveAllAttributes(ICIIDSelection ciSelection, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayerID);
            var relevantAttributes = relevantAttributesForTrait.Concat(ICIModel.NameAttribute).ToHashSet(); // NOTE: we also delete the __name attribute of the CI
            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataCIAndAttributeNameScope(writeLayerID, new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>(),
                ciSelection, NamedAttributesSelection.Build(relevantAttributes)
                ),
                changesetProxy, trans, maskHandlingForRemoval, otherLayersValueHandling);
        }

        private async Task RemoveAllRelations(ICIIDSelection ciSelection, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (!trait.OptionalRelations.IsEmpty())
            {
                var outgoing = new HashSet<(Guid thisCIID, string predicateID)>();
                var incoming = new HashSet<(Guid thisCIID, string predicateID)>();
                var ciids = await ciSelection.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                foreach (var traitRelation in trait.OptionalRelations)
                {
                    var predicateID = traitRelation.RelationTemplate.PredicateID;
                    var isOutgoing = traitRelation.RelationTemplate.DirectionForward;
                    if (isOutgoing)
                        foreach(var ciid in ciids)
                            outgoing.Add((ciid, predicateID));
                    else
                        foreach (var ciid in ciids)
                            incoming.Add((ciid, predicateID));
                }
                var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayerID);
                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID,
                    new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>(),
                    outgoing, true), changesetProxy, trans, maskHandlingForRemoval, otherLayersValueHandling);
                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID,
                    new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>(),
                    incoming, false), changesetProxy, trans, maskHandlingForRemoval, otherLayersValueHandling);
            }
        }

        public async Task<(EffectiveTrait et, bool changed)> SetRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDs, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var relevantRelations = new HashSet<(Guid thisCIID, string predicateID)>() { (thisCIID, tr.RelationTemplate.PredicateID) };
            var relations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>() { (thisCIID, predicateID: tr.RelationTemplate.PredicateID, relatedCIIDs) };
            var scope = new BulkRelationDataCIAndPredicateScope(writeLayerID, relations, relevantRelations, tr.RelationTemplate.DirectionForward);
            var changed = await WriteRelations(scope, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot set relations of trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }

        public async Task<(EffectiveTrait et, bool changed)> AddRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDsToAdd, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var fragments = (tr.RelationTemplate.DirectionForward)
                ? relatedCIIDsToAdd.Select(ciid => new BulkRelationFullFragment(thisCIID, ciid, tr.RelationTemplate.PredicateID, false))
                : relatedCIIDsToAdd.Select(ciid => new BulkRelationFullFragment(ciid, thisCIID, tr.RelationTemplate.PredicateID, false));
            var scope = new BulkRelationDataSpecificScope(writeLayerID, fragments, ImmutableList<(Guid from, Guid to, string predicateID)>.Empty);

            var changed = await WriteRelations(scope, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot add relations to trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }

        public async Task<(EffectiveTrait et, bool changed)> RemoveRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDsToRemove, LayerSet layerSet, string writeLayerID, IChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var toRemove = (tr.RelationTemplate.DirectionForward)
                ? relatedCIIDsToRemove.Select(ciid => (thisCIID, ciid, tr.RelationTemplate.PredicateID))
                : relatedCIIDsToRemove.Select(ciid => (ciid, thisCIID, tr.RelationTemplate.PredicateID));
            var scope = new BulkRelationDataSpecificScope(writeLayerID, ImmutableList<BulkRelationFullFragment>.Empty, toRemove);

            var changed = await WriteRelations(scope, layerSet, writeLayerID, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot remove relations from trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }
    }
}
