using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public abstract class TraitDataConfigBaseModel<T>
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly GenericTrait trait;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public TraitDataConfigBaseModel(GenericTrait trait, IBaseConfigurationModel baseConfigurationModel, IEffectiveTraitModel effectiveTraitModel)
        {
            this.trait = trait;
            this.baseConfigurationModel = baseConfigurationModel;
            this.effectiveTraitModel = effectiveTraitModel;
        }

        protected async Task<T> Get(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            var t = await TryToGet(id, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find {typeof(T).Name} with ID {id}");
            } else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid,T)> TryToGet(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var CIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(trait, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

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

        public async Task<IDictionary<string, T>> GetAll(IModelContext trans, TimeThreshold timeThreshold)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var CIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(trait, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new Dictionary<string, T>();
            foreach(var (_, et) in CIs.Values)
            {
                var (dc, id) = EffectiveTrait2DC(et);
                ret.Add(id, dc);
            }
            return ret;
        }
    }
}
