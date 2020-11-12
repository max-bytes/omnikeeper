using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
    public class EffectiveTraitSetDTO
    {
        [Required] public CIDTO UnderlyingCI { get; set; }
        [Required] public IImmutableDictionary<string, EffectiveTraitDTO> EffectiveTraits { get; set; }

        private EffectiveTraitSetDTO(CIDTO underlyingCI, IImmutableDictionary<string, EffectiveTraitDTO> effectiveTraits)
        {
            UnderlyingCI = underlyingCI;
            EffectiveTraits = effectiveTraits;
        }

        public static EffectiveTraitSetDTO BuildFromETS(EffectiveTraitSet traitSet)
        {
            return new EffectiveTraitSetDTO(CIDTO.BuildFromMergedCI(traitSet.UnderlyingCI),
                traitSet.EffectiveTraits.Select(kv => (kv.Key, EffectiveTraitDTO.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            );
        }
    }

    public class EffectiveTraitDTO
    {
        private EffectiveTraitDTO(IImmutableDictionary<string, CIAttributeDTO> traitAttributes, IImmutableDictionary<string, IEnumerable<RelatedCIDTO>> traitRelations)
        {
            TraitAttributes = traitAttributes;
            TraitRelations = traitRelations;
        }

        [Required] public IImmutableDictionary<string, CIAttributeDTO> TraitAttributes { get; set; }
        [Required] public IImmutableDictionary<string, IEnumerable<RelatedCIDTO>> TraitRelations { get; set; }

        public static EffectiveTraitDTO Build(EffectiveTrait et)
        {
            return new EffectiveTraitDTO(
                et.TraitAttributes.Select(kv => (kv.Key, CIAttributeDTO.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2),
                et.TraitRelations.Select(kv => (kv.Key, kv.Value.Select(r => new RelatedCIDTO(r)))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            );
        }
    }

}
