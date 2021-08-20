using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    // TODO: think about caching?
    public class RecursiveDataTraitModel : IRecursiveDataTraitModel
    {
        private readonly IEffectiveTraitModel effectiveTraitModel;
        private readonly IBaseConfigurationModel baseConfigurationModel;

        public RecursiveDataTraitModel(IEffectiveTraitModel effectiveTraitModel, IBaseConfigurationModel baseConfigurationModel)
        {
            this.effectiveTraitModel = effectiveTraitModel;
            this.baseConfigurationModel = baseConfigurationModel;
        }

        public async Task<RecursiveTrait> GetRecursiveTrait(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            var t = await TryToGetRecursiveTrait(id, timeThreshold, trans);
            if (t.Equals(default))
            {
                throw new Exception($"Could not find recursive trait with ID {id}");
            }
            else
            {
                return t.Item2;
            }
        }

        public async Task<(Guid, RecursiveTrait)> TryToGetRecursiveTrait(string id, TimeThreshold timeThreshold, IModelContext trans)
        {
            IDValidations.ValidateTraitIDThrow(id);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            // TODO: better performance possible?
            var traitCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.TraitFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);

            var foundTraitCIs = traitCIs
                .Where(pci => TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(pci.Value.et, "id") == id)
                .OrderBy(t => t.Key); // we order by GUID to stay consistent even when multiple CIs would match

            var foundTraitCI = foundTraitCIs.FirstOrDefault();
            if (!foundTraitCI.Equals(default(KeyValuePair<Guid, (MergedCI ci, EffectiveTrait et)>)))
            {
                return (foundTraitCI.Key, EffectiveTrait2RecursiveTrait(foundTraitCI.Value.et));
            }
            return default;
        }

        public async Task<IEnumerable<RecursiveTrait>> GetRecursiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var traitCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(CoreTraits.TraitFlattened, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new List<RecursiveTrait>();
            foreach(var (ci, trait) in traitCIs.Values)
            {
                ret.Add(EffectiveTrait2RecursiveTrait(trait));
            }
            return ret;
        }

        private RecursiveTrait EffectiveTrait2RecursiveTrait(EffectiveTrait trait)
        {
            var traitID = TraitConfigDataUtils.ExtractMandatoryScalarTextAttribute(trait, "id");

            var requiredAttributes = TraitConfigDataUtils.DeserializeMandatoryArrayJSONAttribute(trait, "required_attributes", TraitAttribute.Serializer);
            var optionalAttributes = TraitConfigDataUtils.DeserializeOptionalArrayJSONAttribute(trait, "optional_attributes", TraitAttribute.Serializer, new List<TraitAttribute>());
            var requiredRelations = TraitConfigDataUtils.DeserializeOptionalArrayJSONAttribute(trait, "required_relation", TraitRelation.Serializer, new List<TraitRelation>());

            var requiredTraits = TraitConfigDataUtils.ExtractOptionalArrayTextAttribute(trait, "required_traits", new string[0]);

            return new RecursiveTrait(traitID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, requiredTraits);
        }
    }
}
