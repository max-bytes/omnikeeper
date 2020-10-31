using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class EffectiveTrait
    {
        public Trait UnderlyingTrait { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> TraitAttributes { get; private set; }
        public IImmutableDictionary<string, IEnumerable<MergedRelatedCI>> TraitRelations { get; private set; }

        public static EffectiveTrait Build(Trait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<MergedRelatedCI>> traitRelations)
        {
            return new EffectiveTrait
            {
                UnderlyingTrait = underlyingTrait,
                TraitAttributes = traitAttributes.ToImmutableDictionary(),
                TraitRelations = traitRelations.ToImmutableDictionary()
            };
        }
    }
}
