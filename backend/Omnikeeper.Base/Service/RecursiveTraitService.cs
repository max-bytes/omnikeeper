using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Service
{
    public static class RecursiveTraitService
    {
        public static IDictionary<string, GenericTrait> FlattenRecursiveTraits(IEnumerable<RecursiveTrait> rts)
        {
            var dict = rts.ToDictionary(rt => rt.ID);
            return FlattenRecursiveTraits(dict);
        }

        public static IDictionary<string, GenericTrait> FlattenRecursiveTraits(IDictionary<string, RecursiveTrait> input, Action<string> errorF)
        {
            var flattened = new Dictionary<string, GenericTrait>();
            var unflattened = new Dictionary<string, RecursiveTrait>(input);
            foreach (var kvi in input)
            {
                FlattenDependentTraitsRec(kvi.Value, flattened, unflattened, errorF);
            }
            return flattened;
        }

        public static IDictionary<string, GenericTrait> FlattenRecursiveTraits(IDictionary<string, RecursiveTrait> input)
        {
            return FlattenRecursiveTraits(input, (_) => { });
        }

        public static GenericTrait FlattenSingleRecursiveTrait(RecursiveTrait rt)
        {
            return FlattenDependentTraitsRec(rt, new Dictionary<string, GenericTrait>(), new Dictionary<string, RecursiveTrait>(), (_) => { });
        }

        private static GenericTrait FlattenDependentTraitsRec(RecursiveTrait trait, IDictionary<string, GenericTrait> flattened, IDictionary<string, RecursiveTrait> unflattened, Action<string> errorF)
        {
            if (flattened.ContainsKey(trait.ID)) return flattened[trait.ID];

            unflattened.Remove(trait.ID);

            var flattenedDependencies = new List<GenericTrait>();
            foreach (var rts in trait.RequiredTraits)
            {
                if (flattened.TryGetValue(rts, out var resolvedRT))
                    flattenedDependencies.Add(resolvedRT);
                else if (unflattened.TryGetValue(rts, out var unflattenedRT))
                { // dependency is not yet resolved, recursively resolve
                    var flattenedRT = FlattenDependentTraitsRec(unflattenedRT, flattened, unflattened, errorF);
                    flattenedDependencies.Add(flattenedRT);
                }
                else
                {
                    // we hit a loop! stop recursing
                    errorF($"Hit a loop while flattening trait with ID {trait.ID}");
                }
            }

            // merge flattened dependencies and current recursive trait into one single trait
            // NOTE: it is possible that multiple requirements share the same identifier; this needs to be resorted because the identifier must be unique per trait attribute/relation.
            // To disambiguate in cases of duplicate trait identifiers, there is a priority that governs which requirement gets picked, and the rest are dropped
            // this priority goes as follows:
            // 1) within the trait itself, the priority of requirements is defined by their order of appearance in the definition (this is relevant in case a trait itself has multiple requirements with the same identifier)
            // 2) requirements of the trait itself have a higher priority than requirements from required traits
            // 3) the priority of requirements from required traits is resolved by their order of appearance in the required-traits array
            // 4) the priority of requirements within a required trait is resolved by their order of appearance in the definition
            var requiredAttributes = trait.RequiredAttributes.Concat(flattenedDependencies.SelectMany(d => d.RequiredAttributes)).GroupBy(d => d.Identifier).Select(l => l.First());
            var optionalAttributes = trait.OptionalAttributes.Concat(flattenedDependencies.SelectMany(d => d.OptionalAttributes)).GroupBy(d => d.Identifier).Select(l => l.First());
            var optionalRelations = trait.OptionalRelations.Concat(flattenedDependencies.SelectMany(d => d.OptionalRelations)).GroupBy(d => d.Identifier).Select(l => l.First());

            // remove the optional requirement when there is a mandatory requirement with the same identifier
            optionalAttributes = optionalAttributes.Where(r => requiredAttributes.All(rr => rr.Identifier != r.Identifier));

            var ancestorTraits = trait.RequiredTraits.Concat(flattenedDependencies.SelectMany(d => d.AncestorTraits)).ToHashSet();
            var flattenedTrait = GenericTrait.Build(trait.ID, trait.Origin, requiredAttributes, optionalAttributes, optionalRelations, ancestorTraits);

            flattened.Add(trait.ID, flattenedTrait);

            return flattenedTrait;
        }
    }
}
