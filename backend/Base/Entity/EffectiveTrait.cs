using System.Collections.Generic;
using System.Collections.Immutable;

namespace Landscape.Base.Entity
{
    public class EffectiveTraitSet
    {
        public MergedCI UnderlyingCI { get; private set; }
        public IImmutableDictionary<string, EffectiveTrait> EffectiveTraits { get; private set; }

        public static EffectiveTraitSet Build(MergedCI underlyingCI, EffectiveTrait effectiveTrait)
        {
            return Build(underlyingCI, new EffectiveTrait[] { effectiveTrait });
        }

        public static EffectiveTraitSet Build(MergedCI underlyingCI, IEnumerable<EffectiveTrait> effectiveTraits)
        {
            return new EffectiveTraitSet
            {
                UnderlyingCI = underlyingCI,
                EffectiveTraits = effectiveTraits.ToImmutableDictionary(et => et.UnderlyingTrait.Name)
            };
        }
    }
    public class EffectiveTrait
    {
        public Trait UnderlyingTrait { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> TraitAttributes { get; private set; }
        public IImmutableDictionary<string, IEnumerable<(Relation relation, MergedCI toCI)>> TraitRelations { get; private set; }
        public IEnumerable<string> DependentTraits { get; private set; }

        public static EffectiveTrait Build(Trait underlyingTrait, 
            IDictionary<string, MergedCIAttribute> traitAttributes, 
            IDictionary<string, IEnumerable<(Relation relation, MergedCI toCI)>> traitRelations,
            IEnumerable<string> dependentTraits)
        {
            return new EffectiveTrait
            {
                UnderlyingTrait = underlyingTrait,
                TraitAttributes = traitAttributes.ToImmutableDictionary(),
                TraitRelations = traitRelations.ToImmutableDictionary(),
                DependentTraits = dependentTraits
            };
        }
    }
}
