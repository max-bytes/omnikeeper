using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
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
                .Where(pci => pci.Value.et.TraitAttributes["id"].Attribute.Value.Value2String() == id)
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
            var idA = trait.TraitAttributes["id"];
            var traitID = idA.Attribute.Value.Value2String();

            IEnumerable<T> DeserializeJSONArrayAttribute<T>(MergedCIAttribute? a, MyJSONSerializer<T> serializer) where T : class
            {
                if (a == null) // empty / no attribute
                    return new List<T>();
                var raa = a.Attribute.Value as AttributeArrayValueJSON;
                if (raa == null)
                {
                    throw new Exception("Invalid trait configuration");
                }
                return raa.Values.Select(v =>
                {
                    var vo = v.Value as JObject;
                    if (vo == null)
                        throw new Exception("Invalid trait configuration");
                    var s = serializer.Deserialize(vo);
                    if (s == null)
                        throw new Exception("Invalid trait configuration");
                    return s;
                });
            }
            var requiredAttributes = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("requiredAttributes"), TraitAttribute.Serializer);
            var optionalAttributes = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("optionalAttributes"), TraitAttribute.Serializer);
            var requiredRelations = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("requiredRelation"), TraitRelation.Serializer);

            IEnumerable<string> DeserializeTextArrayAttribute(MergedCIAttribute? a)
            {
                if (a == null) // empty / no attribute
                    return new List<string>();
                var raa = a?.Attribute?.Value as AttributeArrayValueText;
                if (raa == null)
                {
                    throw new Exception("Invalid trait configuration");
                }
                return raa.Values.Select(v => v.Value);
            }
            var requiredTraits = DeserializeTextArrayAttribute(trait.TraitAttributes.GetValueOrDefault("requiredTraits"));

            return new RecursiveTrait(traitID, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, requiredTraits);
        }
    }
}
