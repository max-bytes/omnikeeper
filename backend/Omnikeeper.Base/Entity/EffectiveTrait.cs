using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class EffectiveTraitSet
    {
        public MergedCI UnderlyingCI { get; private set; }
        public IImmutableDictionary<string, EffectiveTrait> EffectiveTraits { get; private set; }

        public static EffectiveTraitSet BuildFromSingleET(MergedCI underlyingCI, EffectiveTrait effectiveTrait)
        {
            return new EffectiveTraitSet(underlyingCI, new EffectiveTrait[] { effectiveTrait });
        }

        public EffectiveTraitSet(MergedCI underlyingCI, IEnumerable<EffectiveTrait> effectiveTraits)
        {
            UnderlyingCI = underlyingCI;
            EffectiveTraits = effectiveTraits.ToImmutableDictionary(et => et.UnderlyingTrait.Name);
        }
    }
    public class EffectiveTrait
    {
        public Trait UnderlyingTrait { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> TraitAttributes { get; private set; }
        public IImmutableDictionary<string, IEnumerable<MergedRelatedCI>> TraitRelations { get; private set; }

        public EffectiveTrait(Trait underlyingTrait,
            IDictionary<string, MergedCIAttribute> traitAttributes,
            IDictionary<string, IEnumerable<MergedRelatedCI>> traitRelations)
        {
            UnderlyingTrait = underlyingTrait;
            TraitAttributes = traitAttributes.ToImmutableDictionary();
            TraitRelations = traitRelations.ToImmutableDictionary();
        }
    }
}
