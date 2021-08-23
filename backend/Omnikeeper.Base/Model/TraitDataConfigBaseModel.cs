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
    public abstract class TraitDataConfigBaseModel<T>
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly ICIModel ciModel;
        private readonly IBaseAttributeModel baseAttributeModel;
        private readonly GenericTrait trait;

        public TraitDataConfigBaseModel(GenericTrait trait, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IBaseAttributeModel baseAttributeModel)
        {
            this.trait = trait;
            this.effectiveTraitModel = effectiveTraitModel;
            this.ciModel = ciModel;
            this.baseAttributeModel = baseAttributeModel;
        }

        protected async Task<T> Get(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGet(id, layerSet, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find {typeof(T).Name} with ID {id}");
            } else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid,T)> TryToGet(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            // TODO: better performance possible?
            var CIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(trait, layerSet, new AllCIIDsSelection(), trans, timeThreshold);

            var foundCIs = CIs.Where(pci => TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(pci.Value.et, "id") == id)
                .OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundCI = foundCIs.FirstOrDefault();
            if (!foundCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                var (dc, _) = EffectiveTrait2DC(foundCI.Value.et);
                return (foundCI.Key, dc);
            }
            return default;
        }

        protected abstract (T dc, string id) EffectiveTrait2DC(EffectiveTrait et);

        public async Task<IDictionary<string, T>> GetAll(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold)
        {
            var CIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(trait, layerSet, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, T>();
            foreach(var (_, et) in CIs.Values.OrderBy(t => t.ci.ID)) // we order by GUID to stay consistent even when multiple CIs have the same ID
            {
                var (dc, id) = EffectiveTrait2DC(et);
                try
                {
                    ret.Add(id, dc);
                } catch (ArgumentException)
                { // duplicate detected, do not add
                    // TODO: better duplicate handling possible here?
                }
            }
            return ret;
        }

        protected async Task<(T dc, bool changed)> InsertOrUpdate(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params (string attributeName, IAttributeValue value)[] attributes)
        {
            var t = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);

            Guid ciid = (t.Equals(default)) ? await ciModel.CreateCI(trans) : t.Item1;

            var changed = await TraitConfigDataUtils.WriteAttributes(baseAttributeModel, ciid, writeLayerID, changesetProxy, dataOrigin, trans, attributes);

            try
            {
                var dc = await Get(id, layerSet, changesetProxy.TimeThreshold, trans);
                return (dc, changed);
            }
            catch (Exception)
            {
                throw new Exception("DC does not conform to trait requirements");
            }
        }

        protected async Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans, params string[] attributesToRemove)
        {
            var t = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);
            if (t.Equals(default))
            {
                return false; // no dc with this ID exists
            }

            foreach(var attribute in attributesToRemove)
                await baseAttributeModel.RemoveAttribute(attribute, t.Item1, writeLayerID, changesetProxy, dataOrigin, trans);

            var tAfterDeletion = await TryToGet(id, layerSet, changesetProxy.TimeThreshold, trans);
            return tAfterDeletion.Equals(default); // return successful if dc does not exist anymore afterwards
        }
    }
}
