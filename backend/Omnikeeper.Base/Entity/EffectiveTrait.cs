using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class EffectiveTrait
    {
        public readonly ITrait UnderlyingTrait;
        public readonly IImmutableDictionary<string, MergedCIAttribute> TraitAttributes;
        public readonly IImmutableDictionary<string, IEnumerable<CompactRelatedCI>> TraitRelations;

        public EffectiveTrait(ITrait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<CompactRelatedCI>> traitRelations)
        {
            UnderlyingTrait = underlyingTrait;
            TraitAttributes = traitAttributes.ToImmutableDictionary();
            TraitRelations = traitRelations.ToImmutableDictionary();
        }
    }
}
