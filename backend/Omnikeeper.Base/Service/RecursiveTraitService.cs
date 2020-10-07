using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Service
{
    public static class RecursiveTraitService
    {
        public static TraitSet FlattenRecursiveTraitSet(RecursiveTraitSet rts)
        {
            return TraitSet.Build(FlattenDependentTraits(rts.Traits));
        }

        public static IEnumerable<Trait> FlattenDependentTraits(IReadOnlyDictionary<string, RecursiveTrait> input)
        {
            var flattened = new Dictionary<string, Trait>();
            var unflattened = new Dictionary<string, RecursiveTrait>(input);
            foreach (var kvi in input)
            {
                FlattenDependentTraitsRec(kvi.Value, flattened, unflattened);
            }
            return flattened.Values;
        }

        private static Trait FlattenDependentTraitsRec(RecursiveTrait trait, IDictionary<string, Trait> flattened, IDictionary<string, RecursiveTrait> unflattened)
        {
            if (flattened.ContainsKey(trait.Name)) return flattened[trait.Name];

            unflattened.Remove(trait.Name);

            var flattenedDependencies = new List<Trait>();
            foreach (var rts in trait.RequiredTraits)
            {
                if (flattened.TryGetValue(rts, out var resolvedRT))
                    flattenedDependencies.Add(resolvedRT);
                else if (unflattened.TryGetValue(rts, out var unflattenedRT))
                { // dependency is not yet resolved, recursively resolve
                    var flattenedRT = FlattenDependentTraitsRec(unflattenedRT, flattened, unflattened);
                    flattenedDependencies.Add(flattenedRT);
                }
                else
                {
                    // we hit a loop! stop recursing
                    // TODO: what to do? Not adding the dependency is a good start
                }
            }

            // merge flattened dependencies and current recursive trait into one single trait
            var requiredAttributes = trait.RequiredAttributes.Concat(flattenedDependencies.SelectMany(d => d.RequiredAttributes));
            var optionalAttributes = trait.OptionalAttributes.Concat(flattenedDependencies.SelectMany(d => d.OptionalAttributes));
            var requiredRelations = trait.RequiredRelations.Concat(flattenedDependencies.SelectMany(d => d.RequiredRelations));
            var flattenedTrait = Trait.Build(trait.Name, requiredAttributes, optionalAttributes, requiredRelations);

            flattened.Add(trait.Name, flattenedTrait);

            return flattenedTrait;
        }

    }
}
