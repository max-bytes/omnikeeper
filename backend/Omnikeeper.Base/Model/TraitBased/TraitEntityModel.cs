using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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
        private readonly ITrait trait;
        private readonly HashSet<string> relevantAttributesForTrait;

        public TraitEntityModel(ITrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
        {
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
            this.trait = trait;

            relevantAttributesForTrait = trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Concat(trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet();
        }

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

        /*
         * NOTE: unlike the regular update, this does not do any checks if the updated entities actually fulfill the trait requirements 
         * and will be considered as this trait's entities going forward
         */
        // NOTE: the cis MUST exist already
        public async Task<bool> BulkReplace(IReadOnlySet<Guid> relevantCIIDs, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (attributeFragments.IsEmpty() || relevantCIIDs.IsEmpty())
                return false;

            var changed = await WriteAttributes(attributeFragments, relevantCIIDs, relevantAttributesForTrait, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            if (!trait.OptionalRelations.IsEmpty())
            {
                var relevantOutgoingRelations = trait.OptionalRelations.Where(rr => rr.RelationTemplate.DirectionForward).SelectMany(rr => relevantCIIDs.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();
                var outgoingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true);
                var tmpChanged = await WriteRelations(outgoingScope, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;

                var relevantIncomingRelations = trait.OptionalRelations.Where(rr => !rr.RelationTemplate.DirectionForward).SelectMany(rr => relevantCIIDs.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();
                var incomingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false);
                tmpChanged = await WriteRelations(incomingScope, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;
            }

            return changed;
        }

        // NOTE: the ci MUST exist already
        public async Task<(EffectiveTrait et, bool changed)> InsertOrUpdateFull(Guid ciid, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            string? ciName, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var relevantAttributes = relevantAttributesForTrait;

            if (ciName != null)
            {
                relevantAttributes = relevantAttributes.Concat(ICIModel.NameAttribute).ToHashSet();
                attributeFragments = attributeFragments.Concat(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, ICIModel.NameAttribute, new AttributeScalarValueText(ciName)));
            }

            var changed = await WriteAttributes(attributeFragments, new HashSet<Guid>() { ciid }, relevantAttributes, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            if (!trait.OptionalRelations.IsEmpty())
            {
                var relevantOutgoingRelations = trait.OptionalRelations.Where(rr => rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var outgoingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true);
                var tmpChanged = await WriteRelations(outgoingScope, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;

                var relevantIncomingRelations = trait.OptionalRelations.Where(rr => !rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var incomingScope = new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false);
                tmpChanged = await WriteRelations(incomingScope, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;
            }

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("DC does not conform to trait requirements");
            return (dc, changed);
        }

        public async Task<(EffectiveTrait et, bool changed)> InsertOrUpdateAttributesOnly(Guid ciid, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            string? ciName, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var relevantAttributes = relevantAttributesForTrait;

            if (ciName != null)
            {
                relevantAttributes = relevantAttributes.Concat(ICIModel.NameAttribute).ToHashSet();
                attributeFragments = attributeFragments.Concat(new BulkCIAttributeDataCIAndAttributeNameScope.Fragment(ciid, ICIModel.NameAttribute, new AttributeScalarValueText(ciName)));
            }

            var changed = await WriteAttributes(attributeFragments, new HashSet<Guid>() { ciid }, relevantAttributes, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("DC does not conform to trait requirements");
            return (dc, changed);
        }

        private async Task<bool> WriteAttributes(IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> fragments,
            IReadOnlySet<Guid> relevantCIs, IReadOnlySet<string> relevantAttributes, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans,
            IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
            var numChanged = await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataCIAndAttributeNameScope(writeLayer, fragments, relevantCIs, relevantAttributes), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);

            return numChanged > 0;
        }

        private async Task<bool> WriteRelations<F>(IBulkRelationData<F> scope, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
            var tmpNumChanged = await relationModel.BulkReplaceRelations(scope, changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
            return tmpNumChanged > 0;
        }

        // NOTE: assumes that the ciid exists, does not check beforehand if the trait entity is actually present
        // NOTE: also deletes the __name ci attribute, if it exists
        public async Task<bool> TryToDelete(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            await RemoveAllAttributes(ciid, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
            await RemoveAllRelations(ciid, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            var dcAfterDeletion = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            return (dcAfterDeletion == null); // return successful if dc does not exist anymore afterwards
        }

        private async Task RemoveAllAttributes(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayerID);
            var relevantAttributes = relevantAttributesForTrait.Concat(ICIModel.NameAttribute).ToHashSet(); // NOTE: we also delete the __name attribute of the CI
            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataCIAndAttributeNameScope(writeLayerID, new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>(),
                new HashSet<Guid>() { ciid },
                relevantAttributes
                ),
                changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
        }

        private async Task RemoveAllRelations(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (!trait.OptionalRelations.IsEmpty())
            {
                var outgoing = new HashSet<(Guid thisCIID, string predicateID)>();
                var incoming = new HashSet<(Guid thisCIID, string predicateID)>();
                foreach (var traitRelation in trait.OptionalRelations)
                {
                    var predicateID = traitRelation.RelationTemplate.PredicateID;
                    var isOutgoing = traitRelation.RelationTemplate.DirectionForward;
                    if (isOutgoing)
                        outgoing.Add((ciid, predicateID));
                    else
                        incoming.Add((ciid, predicateID));
                }
                var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayerID);
                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID,
                    new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>(),
                    outgoing, true), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
                await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayerID,
                    new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>(),
                    incoming, false), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
            }
        }

        public async Task<(EffectiveTrait et, bool changed)> SetRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDs, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOriginV1, ChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var relevantRelations = new HashSet<(Guid thisCIID, string predicateID)>() { (thisCIID, tr.RelationTemplate.PredicateID) };
            var relations = new List<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)>() { (thisCIID, predicateID: tr.RelationTemplate.PredicateID, relatedCIIDs) };
            var scope = new BulkRelationDataCIAndPredicateScope(writeLayerID, relations, relevantRelations, tr.RelationTemplate.DirectionForward);
            var changed = await WriteRelations(scope, layerSet, writeLayerID, dataOriginV1, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot set relations of trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }

        public async Task<(EffectiveTrait et, bool changed)> AddRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDsToAdd, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOriginV1, ChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var fragments = (tr.RelationTemplate.DirectionForward)
                ? relatedCIIDsToAdd.Select(ciid => new BulkRelationDataSpecificScope.Fragment(thisCIID, ciid, tr.RelationTemplate.PredicateID, false))
                : relatedCIIDsToAdd.Select(ciid => new BulkRelationDataSpecificScope.Fragment(ciid, thisCIID, tr.RelationTemplate.PredicateID, false));
            var scope = new BulkRelationDataSpecificScope(writeLayerID, fragments, ImmutableList<(Guid from, Guid to, string predicateID)>.Empty);

            var changed = await WriteRelations(scope, layerSet, writeLayerID, dataOriginV1, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot add relations to trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }

        public async Task<(EffectiveTrait et, bool changed)> RemoveRelations(TraitRelation tr, Guid thisCIID, Guid[] relatedCIIDsToRemove, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOriginV1, ChangesetProxy changesetProxy, IModelContext trans, MaskHandlingForRemovalApplyNoMask maskHandlingForRemoval)
        {
            var toRemove = (tr.RelationTemplate.DirectionForward)
                ? relatedCIIDsToRemove.Select(ciid => (thisCIID, ciid, tr.RelationTemplate.PredicateID))
                : relatedCIIDsToRemove.Select(ciid => (ciid, thisCIID, tr.RelationTemplate.PredicateID));
            var scope = new BulkRelationDataSpecificScope(writeLayerID, ImmutableList<BulkRelationDataSpecificScope.Fragment>.Empty, toRemove);

            var changed = await WriteRelations(scope, layerSet, writeLayerID, dataOriginV1, changesetProxy, trans, maskHandlingForRemoval);

            var dc = await GetSingleByCIID(thisCIID, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("Cannot remove relations from trait entity: trait entity does not conform to trait requirements");
            return (dc, changed);
        }
    }
}
