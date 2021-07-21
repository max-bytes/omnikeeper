using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Service
{
    public static class RecursiveTraitService
    {
        public static TraitSet FlattenRecursiveTraits(IEnumerable<RecursiveTrait> rts)
        {
            var dict = rts.ToDictionary(rt => rt.Name);
            return TraitSet.Build(FlattenDependentTraits(dict));
        }

        public static IEnumerable<GenericTrait> FlattenDependentTraits(IDictionary<string, RecursiveTrait> input)
        {
            var flattened = new Dictionary<string, GenericTrait>();
            var unflattened = new Dictionary<string, RecursiveTrait>(input);
            foreach (var kvi in input)
            {
                FlattenDependentTraitsRec(kvi.Value, flattened, unflattened);
            }
            return flattened.Values;
        }

        public static GenericTrait FlattenSingleRecursiveTrait(RecursiveTrait rt)
        {
            return FlattenDependentTraitsRec(rt, new Dictionary<string, GenericTrait>(), new Dictionary<string, RecursiveTrait>());
        }

        private static GenericTrait FlattenDependentTraitsRec(RecursiveTrait trait, IDictionary<string, GenericTrait> flattened, IDictionary<string, RecursiveTrait> unflattened)
        {
            if (flattened.ContainsKey(trait.Name)) return flattened[trait.Name];

            unflattened.Remove(trait.Name);

            var flattenedDependencies = new List<GenericTrait>();
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
            var ancestorTraits = trait.RequiredTraits.Concat(flattenedDependencies.SelectMany(d => d.AncestorTraits)).ToHashSet();
            var flattenedTrait = GenericTrait.Build(trait.Name, trait.Origin, requiredAttributes, optionalAttributes, requiredRelations, ancestorTraits);

            flattened.Add(trait.Name, flattenedTrait);

            return flattenedTrait;
        }

    }
}
