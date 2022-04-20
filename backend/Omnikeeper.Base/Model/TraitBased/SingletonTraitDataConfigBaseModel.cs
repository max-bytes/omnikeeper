using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    // TODO: refactor to be more like GenericTraitEntityModel
    public abstract class SingletonTraitDataConfigBaseModel<T> where T : TraitEntity, new()
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IAttributeModel attributeModel;
        protected readonly IRelationModel relationModel;
        private readonly GenericTrait trait;
        private readonly HashSet<string> relevantAttributesForTrait;

        public SingletonTraitDataConfigBaseModel(GenericTrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel)
        {
            this.trait = trait;
            relevantAttributesForTrait = trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Concat(trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet();
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.attributeModel = attributeModel;
            this.relationModel = relationModel;
        }

        protected async Task<T> Get(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGet(layerSet, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find {typeof(T).Name}");
            }
            else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid, T)> TryToGet(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, NamedAttributesSelection.Build(relevantAttributesForTrait), trans, timeThreshold);
            var foundCIs = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            var sortedCIs = foundCIs.OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match
            var foundCI = sortedCIs.FirstOrDefault();
            if (!foundCI.Equals(default(KeyValuePair<Guid, EffectiveTrait>)))
            {
                var dc = GenericTraitEntityHelper.EffectiveTrait2Object<T>(foundCI.Value);
                return (foundCI.Key, dc);
            }
            return default;
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributes(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params (string attributeName, IAttributeValue value)[] attributes)
        {
            return await InsertOrUpdateAttributesAndRelations(layerSet, writeLayerID, dataOrigin, changesetProxy, trans, attributes, new (Guid, bool, string)[0]);
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributesAndRelations(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans,
            IEnumerable<(string attributeName, IAttributeValue value)> attributes, IEnumerable<(Guid otherCIID, bool forward, string predicateID)> relations)
        {
            var t = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await ciModel.CreateCI(trans) : t.Item1;

            var otherLayersValueHandling = OtherLayersValueHandlingForceWrite.Instance;

            var changed = false;
            foreach (var (attributeName, value) in attributes)
            {
                if (value != null)
                {
                    var tmpChanged = await attributeModel.InsertAttribute(attributeName, value, ciid, writeLayerID, changesetProxy, dataOrigin, trans, otherLayersValueHandling);
                    changed = changed || tmpChanged;
                }
            }

            foreach (var (otherCIID, forward, predicateID) in relations)
            {
                if (predicateID != default)
                {
                    var fromCIID = (forward) ? ciid : otherCIID;
                    var toCIID = (forward) ? otherCIID : ciid;
                    var tmpChanged = await relationModel.InsertRelation(fromCIID, toCIID, predicateID, false, writeLayerID, changesetProxy, dataOrigin, trans, otherLayersValueHandling);
                    changed = changed || tmpChanged;
                }
            }

            try
            {
                var dc = await Get(layerSet, changesetProxy.TimeThreshold, trans);
                return (dc, changed);
            }
            catch (Exception e)
            {
                throw new Exception("DC does not conform to trait requirements", e);
            }
        }

        protected async Task<bool> TryToDelete(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params string[] attributesToRemove)
        {
            return await TryToDelete(layerSet, writeLayerID, dataOrigin, changesetProxy, trans, attributesToRemove, new string[0], new string[0]);
        }

        protected async Task<bool> TryToDelete(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy,
            IModelContext trans, IEnumerable<string> attributesToRemove,
            IEnumerable<string> relationsToRemoveForward,
            IEnumerable<string> relationsToRemoveBackward)
        {
            var t = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no dc exists
            }

            foreach (var attribute in attributesToRemove)
            {
                var changed = await attributeModel.RemoveAttribute(attribute, t.Item1, writeLayerID, changesetProxy, dataOrigin, trans, MaskHandlingForRemovalApplyNoMask.Instance);
            }

            // TODO: masking
            var maskHandlingForRetrieval = MaskHandlingForRetrievalApplyMasks.Instance;
            var maskHandlingForRemoval = MaskHandlingForRemovalApplyNoMask.Instance;

            var allRelationsForward = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(t.Item1), new LayerSet(writeLayerID), trans, TimeThreshold.BuildLatest(), maskHandlingForRetrieval, GeneratedDataHandlingInclude.Instance);
            var allRelationsBackward = await relationModel.GetMergedRelations(RelationSelectionTo.Build(t.Item1), new LayerSet(writeLayerID), trans, TimeThreshold.BuildLatest(), maskHandlingForRetrieval, GeneratedDataHandlingInclude.Instance);

            var relevantRelationsForward = allRelationsForward.Select(r => r.Relation).Where(r => relationsToRemoveForward.Contains(r.PredicateID));
            var relevantRelationsBackward = allRelationsBackward.Select(r => r.Relation).Where(r => relationsToRemoveBackward.Contains(r.PredicateID));

            var relationsToRemove = relevantRelationsForward.Concat(relevantRelationsBackward);

            foreach (var r in relationsToRemove)
            {
                var changed = await relationModel.RemoveRelation(r.FromCIID, r.ToCIID, r.PredicateID, writeLayerID, changesetProxy, dataOrigin, trans, maskHandlingForRemoval);
            }

            var tAfterDeletion = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successful if dc does not exist anymore afterwards
        }
    }
}
