using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
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
            if (ci == null) return default;
            var ciWithTrait = await effectiveTraitModel.GetEffectiveTraitForCI(ci, trait, layerSet, trans, timeThreshold);
            return ciWithTrait;
        }

        public async Task<IDictionary<Guid, EffectiveTrait>> GetAllByCIID(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            return cisWithTrait;
        }

        /*
         * NOTE: unlike the regular update, this does not do any checks if the updated entities actually fulfill the trait requirements 
         * and will be considered as this trait's entities going forward
         */
        // NOTE: the cis MUST exist already
        public async Task<bool> BulkReplace(ISet<Guid> relevantCIIDs, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            if (attributeFragments.IsEmpty() || relevantCIIDs.IsEmpty())
                return false;

            var changed = await WriteAttributes(attributeFragments, relevantCIIDs, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            if (!trait.OptionalRelations.IsEmpty())
            {
                var relevantOutgoingRelations = trait.OptionalRelations.Where(rr => rr.RelationTemplate.DirectionForward).SelectMany(rr => relevantCIIDs.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();
                var relevantIncomingRelations = trait.OptionalRelations.Where(rr => !rr.RelationTemplate.DirectionForward).SelectMany(rr => relevantCIIDs.Select(ciid => (ciid, rr.RelationTemplate.PredicateID))).ToHashSet();

                var tmpChanged = await WriteRelations(outgoingRelations, incomingRelations, relevantOutgoingRelations, relevantIncomingRelations, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;
            }

            return changed;
        }

        // NOTE: the ci MUST exist already
        public async Task<(EffectiveTrait et, bool changed)> InsertOrUpdate(Guid ciid, IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> attributeFragments,
            IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var changed = await WriteAttributes(attributeFragments, new HashSet<Guid>() { ciid }, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            if (!trait.OptionalRelations.IsEmpty())
            {
                var relevantOutgoingRelations = trait.OptionalRelations.Where(rr => rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var relevantIncomingRelations = trait.OptionalRelations.Where(rr => !rr.RelationTemplate.DirectionForward).Select(rr => (ciid, rr.RelationTemplate.PredicateID)).ToHashSet();
                var tmpChanged = await WriteRelations(outgoingRelations, incomingRelations, relevantOutgoingRelations, relevantIncomingRelations, layerSet, writeLayer, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
                changed = changed || tmpChanged;
            }

            var dc = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            if (dc == null)
                throw new Exception("DC does not conform to trait requirements");
            return (dc, changed);
        }

        private async Task<bool> WriteAttributes(IEnumerable<BulkCIAttributeDataCIAndAttributeNameScope.Fragment> fragments, ISet<Guid> relevantCIs, LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, 
            IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
            var changed = await attributeModel.BulkReplaceAttributes(new BulkCIAttributeDataCIAndAttributeNameScope(writeLayer, fragments, relevantCIs, relevantAttributesForTrait), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);

            return changed;
        }

        private async Task<bool> WriteRelations(IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> outgoingRelations, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> incomingRelations,
            ISet<(Guid thisCIID, string predicateID)> relevantOutgoingRelations, ISet<(Guid thisCIID, string predicateID)> relevantIncomingRelations,
            LayerSet layerSet, string writeLayer, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var changed = false;
            if (!trait.OptionalRelations.IsEmpty())
            {
                var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayer);
                var tmpChanged = await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayer, outgoingRelations, relevantOutgoingRelations, true), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
                changed = changed || !tmpChanged.IsEmpty();
                tmpChanged = await relationModel.BulkReplaceRelations(new BulkRelationDataCIAndPredicateScope(writeLayer, incomingRelations, relevantIncomingRelations, false), changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
                changed = changed || !tmpChanged.IsEmpty();
            }

            return changed;
        }

        // NOTE: assumes that the ciid exists, does not check beforehand if the trait entity is actually present
        public async Task<bool> TryToDelete(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            await RemoveAttributes(ciid, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);
            await RemoveRelations(ciid, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, maskHandlingForRemoval);

            var dcAfterDeletion = await GetSingleByCIID(ciid, layerSet, trans, changesetProxy.TimeThreshold);
            return (dcAfterDeletion == null); // return successful if dc does not exist anymore afterwards
        }

        private async Task RemoveAttributes(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
        {
            var otherLayersValueHandling = GetOtherLayersValueHandling(layerSet, writeLayerID);
            await attributeModel.BulkReplaceAttributes(
                new BulkCIAttributeDataCIAndAttributeNameScope(writeLayerID, new List<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>(),
                new HashSet<Guid>() { ciid }, relevantAttributesForTrait),
                changesetProxy, dataOrigin, trans, maskHandlingForRemoval, otherLayersValueHandling);
        }

        private async Task RemoveRelations(Guid ciid, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, IMaskHandlingForRemoval maskHandlingForRemoval)
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
    }
}
