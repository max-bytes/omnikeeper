using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
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
