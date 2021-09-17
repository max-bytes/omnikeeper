using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
    public class EffectiveTraitDTO
    {
        private EffectiveTraitDTO(IImmutableDictionary<string, CIAttributeDTO> traitAttributes, 
            IImmutableDictionary<string, IEnumerable<RelationDTO>> outgoingTraitRelations,
            IImmutableDictionary<string, IEnumerable<RelationDTO>> incomingTraitRelations)
        {
            TraitAttributes = traitAttributes;
            OutgoingTraitRelations = outgoingTraitRelations;
            IncomingTraitRelations = incomingTraitRelations;
        }

        [Required] public IImmutableDictionary<string, CIAttributeDTO> TraitAttributes { get; set; }
        [Required] public IImmutableDictionary<string, IEnumerable<RelationDTO>> OutgoingTraitRelations { get; set; }
        [Required] public IImmutableDictionary<string, IEnumerable<RelationDTO>> IncomingTraitRelations { get; set; }

        public static EffectiveTraitDTO Build(EffectiveTrait et)
        {
            return new EffectiveTraitDTO(
                et.TraitAttributes.Select(kv => (kv.Key, CIAttributeDTO.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2),
                et.OutgoingTraitRelations.Select(kv => (kv.Key, kv.Value.Select(r => RelationDTO.BuildFromMergedRelation(r)))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2),
                et.IncomingTraitRelations.Select(kv => (kv.Key, kv.Value.Select(r => RelationDTO.BuildFromMergedRelation(r)))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            );
        }
    }

}
