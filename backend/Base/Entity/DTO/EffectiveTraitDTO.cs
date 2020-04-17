using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Landscape.Base.Entity.DTO
{
    public class EffectiveTraitSetDTO
    {
        [Required] public CIDTO UnderlyingCI { get; private set; }
        [Required] public IImmutableDictionary<string, EffectiveTraitDTO> EffectiveTraits { get; private set; }
        public static EffectiveTraitSetDTO Build(EffectiveTraitSet traitSet)
        {
            return new EffectiveTraitSetDTO
            {
                UnderlyingCI = CIDTO.Build(traitSet.UnderlyingCI),
                EffectiveTraits = traitSet.EffectiveTraits.Select(kv => (kv.Key, EffectiveTraitDTO.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            };
        }
    }

    public class EffectiveTraitDTO
    {
        [Required] public IImmutableDictionary<string, CIAttributeDTO> TraitAttributes { get; private set; }
        [Required] public IImmutableDictionary<string, IEnumerable<RelationDTO>> TraitRelations { get; private set; }

        public static EffectiveTraitDTO Build(EffectiveTrait et)
        {
            return new EffectiveTraitDTO
            {
                TraitAttributes = et.TraitAttributes.Select(kv => (kv.Key, CIAttributeDTO.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2),
                TraitRelations = et.TraitRelations.Select(kv => (kv.Key, kv.Value.Select(r => RelationDTO.Build(r.relation, CIDTO.Build(r.toCI))))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            };
        }
    }

}
