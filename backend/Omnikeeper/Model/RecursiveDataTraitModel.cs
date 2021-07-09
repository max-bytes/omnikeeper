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


        private readonly MyJSONSerializer<TraitAttribute> TraitAttributeSerializer = new MyJSONSerializer<TraitAttribute>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });
        private readonly MyJSONSerializer<TraitRelation> TraitRelationSerializer = new MyJSONSerializer<TraitRelation>(() =>
        {
            var s = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            s.Converters.Add(new StringEnumConverter());
            return s;
        });

        public async Task<RecursiveTraitSet> GetRecursiveDataTraitSet(IModelContext trans, TimeThreshold timeThreshold)
        {
            var traitForTraits = CoreTraits.Trait;
            // NOTE: we need to flatten the core trait first... is this the best way? Could we maybe also keep core traits as flattened already?
            var flattenedTraitForTraits = RecursiveTraitService.FlattenSingleRecursiveTrait(traitForTraits);

            // derive config layerset from base config
            var baseConfig = await baseConfigurationModel.GetConfigOrDefault(trans);
            var configLayerset = new LayerSet(baseConfig.ConfigLayerset);

            var traitCIs = await effectiveTraitModel.CalculateEffectiveTraitsForTrait(flattenedTraitForTraits, configLayerset, new AllCIIDsSelection(), trans, timeThreshold);
            var ret = new List<RecursiveTrait>();
            foreach(var (ci, trait) in traitCIs.Values)
            {
                var nameA = trait.TraitAttributes["name"];
                var traitName = nameA.Attribute.Value.Value2String();

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
                var requiredAttributes = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("requiredAttributes"), TraitAttributeSerializer);
                var optionalAttributes = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("optionalAttributes"), TraitAttributeSerializer);
                var requiredRelations = DeserializeJSONArrayAttribute(trait.TraitAttributes.GetValueOrDefault("requiredRelation"), TraitRelationSerializer);

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

                ret.Add(new RecursiveTrait(traitName, new TraitOriginV1(TraitOriginType.Data), requiredAttributes, optionalAttributes, requiredRelations, requiredTraits));
            }
            return RecursiveTraitSet.Build(ret);
        }
    }
}
