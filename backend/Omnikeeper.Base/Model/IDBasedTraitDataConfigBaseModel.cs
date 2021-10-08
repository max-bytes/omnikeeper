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
    public abstract class IDBasedTraitDataConfigBaseModel<T, ID> where ID: notnull
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        protected readonly ICIModel ciModel;
        protected readonly IBaseAttributeModel baseAttributeModel;
        protected readonly IBaseRelationModel baseRelationModel;
        private readonly GenericTrait trait;

        public IDBasedTraitDataConfigBaseModel(GenericTrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel, IBaseRelationModel baseRelationModel)
        {
            this.trait = trait;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
            this.baseRelationModel = baseRelationModel;
        }

        protected async Task<T> Get(ID id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGet(id, layerSet, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find {typeof(T).Name} with ID {id}");
            }
            else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid, T)> TryToGet(ID id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var attributeValue = ID2AttributeValue(id);
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, AllAttributeSelection.Instance, trans, timeThreshold); // TODO: reduce attribute via selection, only fetch trait relevant
            var foundCIs = await effectiveTraitModel.GetEffectiveTraitsWithTraitAttributeValue(trait, IDTraitAttributeIdentifier(), attributeValue, cis, layerSet, trans, timeThreshold);

            var sortedCIs = foundCIs.OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundCI = sortedCIs.FirstOrDefault();
            if (!foundCI.Equals(default(KeyValuePair<Guid, EffectiveTrait>)))
            {
                var (dc, _) = EffectiveTrait2DC(foundCI.Value);
                return (foundCI.Key, dc);
            }
            return default;
        }

        protected abstract (T dc, ID id) EffectiveTrait2DC(EffectiveTrait et);
        protected abstract IAttributeValue ID2AttributeValue(ID id);
        protected virtual string IDTraitAttributeIdentifier() => "id";

        protected virtual async Task<Guid> CreateNewCI(ID id, IModelContext trans) => await ciModel.CreateCI(trans);

        public async Task<IDictionary<ID, T>> GetAll(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var cis = await ciModel.GetMergedCIs(new AllCIIDsSelection(), layerSet, false, AllAttributeSelection.Instance, trans, timeThreshold); // TODO: reduce attribute via selection, only fetch trait relevant
            var cisWithTrait = await effectiveTraitModel.GetEffectiveTraitsForTrait(trait, cis, layerSet, trans, timeThreshold);
            var ret = new Dictionary<ID, T>();
            foreach (var (ciid, et) in cisWithTrait.Select(kv => (kv.Key, kv.Value)).OrderBy(t => t.Key)) // we order by GUID to stay consistent even when multiple CIs have the same ID
            {
                var (dc, id) = EffectiveTrait2DC(et);
                try
                {
                    ret.Add(id, dc);
                }
                catch (ArgumentException)
                { // duplicate detected, do not add
                    // TODO: better duplicate handling possible here?
                }
            }
            return ret;
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributes(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params (string attributeName, IAttributeValue value)[] attributes)
        {
            return await InsertOrUpdateAttributesAndRelations(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, attributes, new (Guid, bool, string)[0]);
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdateAttributesAndRelations(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, 
            IEnumerable<(string attributeName, IAttributeValue value)> attributes, IEnumerable<(Guid otherCIID, bool forward, string predicateID)> relations)
        {
            var t = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await CreateNewCI(id, trans) : t.Item1;

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
                var dc = await Get(id, layerSet, changesetProxy.TimeThreshold, trans);
                return (dc, changed);
            }
            catch (Exception e)
            {
                throw new Exception("DC does not conform to trait requirements", e);
            }
        }

        protected async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params string[] attributesToRemove)
        {
            return await TryToDelete(id, layerSet, writeLayerID, dataOrigin, changesetProxy, trans, attributesToRemove, new string[0], new string[0]);
        }

        protected async Task<bool> TryToDelete(ID id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, 
            IModelContext trans, IEnumerable<string> attributesToRemove,
            IEnumerable<string> relationsToRemoveForward,
            IEnumerable<string> relationsToRemoveBackward)
        {
            var t = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no dc with this ID exists
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

            var tAfterDeletion = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successful if dc does not exist anymore afterwards
        }
    }
}
