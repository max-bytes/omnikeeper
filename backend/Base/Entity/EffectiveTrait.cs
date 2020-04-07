
using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class SimplifiedEffectiveTraitSet
    {
        public SimplifiedCI UnderlyingCI { get; private set; }
        public IImmutableDictionary<string, SimplifiedEffectiveTrait> EffectiveTraits { get; private set; }
        public static SimplifiedEffectiveTraitSet Build(EffectiveTraitSet traitSet)
        {
            return new SimplifiedEffectiveTraitSet
            {
                UnderlyingCI = SimplifiedCI.Build(traitSet.UnderlyingCI),
                EffectiveTraits = traitSet.EffectiveTraits.Select(kv => (kv.Key, SimplifiedEffectiveTrait.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            };
        }
    }
    public class SimplifiedEffectiveTrait
    {
        public IImmutableDictionary<string, SimplifiedCIAttribute> TraitAttributes { get; private set; }
        public IImmutableDictionary<string, IEnumerable<SimplifiedRelation>> TraitRelations { get; private set; }

        public static SimplifiedEffectiveTrait Build(EffectiveTrait et)
        {
            return new SimplifiedEffectiveTrait
            {
                TraitAttributes = et.TraitAttributes.Select(kv => (kv.Key, SimplifiedCIAttribute.Build(kv.Value))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2),
                TraitRelations = et.TraitRelations.Select(kv => (kv.Key, kv.Value.Select(r => SimplifiedRelation.Build(r.relation, SimplifiedCI.Build(r.toCI))))).ToImmutableDictionary(kv => kv.Key, kv => kv.Item2)
            };
        }
    }


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

        public static EffectiveTrait Build(Trait underlyingTrait, IDictionary<string, MergedCIAttribute> traitAttributes, IDictionary<string, IEnumerable<(Relation relation, MergedCI toCI)>> traitRelations)
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
