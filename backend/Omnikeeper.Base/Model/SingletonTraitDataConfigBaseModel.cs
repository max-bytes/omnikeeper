using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public abstract class SingletonTraitDataConfigBaseModel<T>
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IBaseAttributeModel baseAttributeModel;
        protected readonly IBaseRelationModel baseRelationModel;
        private readonly GenericTrait trait;

        public SingletonTraitDataConfigBaseModel(GenericTrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
        {
            this.trait = trait;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.baseRelationModel = baseRelationModel;
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
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, AllAttributeSelection.Instance, trans, timeThreshold); // TODO: reduce attribute via selection, only fetch trait relevant
            var foundCIs = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);

            var sortedCIs = foundCIs.OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundCI = sortedCIs.FirstOrDefault();
            if (!foundCI.Equals(default(KeyValuePair<Guid, EffectiveTrait>)))
            {
                var dc = EffectiveTrait2DC(foundCI.Value);
                return (foundCI.Key, dc);
            }
            return default;
        }

        protected abstract T EffectiveTrait2DC(EffectiveTrait et);

        protected virtual async Task<Guid> CreateNewCI(IModelContext trans) => await ciModel.CreateCI(trans);

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributes(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params (string attributeName, IAttributeValue value)[] attributes)
        {
            return await InsertOrUpdateAttributesAndRelations(layerSet, writeLayerID, dataOrigin, changesetProxy, trans, attributes, new (Guid, bool, string)[0]);
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributesAndRelations(LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, 
            IEnumerable<(string attributeName, IAttributeValue value)> attributes, IEnumerable<(Guid otherCIID, bool forward, string predicateID)> relations)
        {
            var t = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await CreateNewCI(trans) : t.Item1;

            var changed = false;
            foreach (var (attributeName, value) in attributes)
            {
                if (value != null)
                {
                    (_, var tmpChanged) = await baseAttributeModel.InsertAttribute(attributeName, value, ciid, writeLayerID, changesetProxy, dataOrigin, trans);
                    changed = changed || tmpChanged;
                }
            }

            foreach(var (otherCIID, forward, predicateID) in relations)
            {
                if (predicateID != default)
                {
                    var fromCIID = (forward) ? ciid : otherCIID;
                    var toCIID = (forward) ? otherCIID : ciid;
                    (_, var tmpChanged) = await baseRelationModel.InsertRelation(fromCIID, toCIID, predicateID, writeLayerID, changesetProxy, dataOrigin, trans);
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
                var (_, changed) = await baseAttributeModel.RemoveAttribute(attribute, t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);
            }

            var allRelationsForward = await baseRelationModel.GetRelations(RelationSelectionFrom.Build(t.Item1), writeLayerID, trans, TimeThreshold.BuildLatest());
            var allRelationsBackward = await baseRelationModel.GetRelations(RelationSelectionTo.Build(t.Item1), writeLayerID, trans, TimeThreshold.BuildLatest());

            var relevantRelationsForward = allRelationsForward.Where(r => relationsToRemoveForward.Contains(r.PredicateID));
            var relevantRelationsBackward = allRelationsBackward.Where(r => relationsToRemoveBackward.Contains(r.PredicateID));

            var relationsToRemove = relevantRelationsForward.Concat(relevantRelationsBackward);

            foreach (var r in relationsToRemove)
            {
                var (_, changed) = await baseRelationModel.RemoveRelation(r.FromCIID, r.ToCIID, r.PredicateID, writeLayerID, changesetProxy, dataOrigin, trans);
            }

            var tAfterDeletion = await TryToGet(layerSet, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successful if dc does not exist anymore afterwards
        }
    }
}
