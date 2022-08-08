using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public interface ITrait
    {
        public string ID { get; }
        public IImmutableSet<string> AncestorTraits { get; }
        public TraitOriginV1 Origin { get; }

        public IImmutableList<TraitAttribute> RequiredAttributes { get; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; }

        public IImmutableList<TraitRelation> OptionalRelations { get; }
    }

    public static class TraitExtensions
    {
        public static IReadOnlySet<string> GetRelevantAttributeNames(this ITrait trait)
        {
            return trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Concat(trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet();
        }
    }

    /// <summary>
    /// comparable to RecursiveTrait, but with the recursive dependencies resolved and flattened
    /// </summary>
    public class GenericTrait : ITrait
    {
        private GenericTrait(string id, TraitOriginV1 origin, IImmutableList<TraitAttribute> requiredAttributes, IImmutableList<TraitAttribute> optionalAttributes,
            IImmutableList<TraitRelation> optionalRelations, IImmutableSet<string> ancestorTraits)
        {
            ID = id;
            Origin = origin;
            RequiredAttributes = requiredAttributes;
            OptionalAttributes = optionalAttributes;
            OptionalRelations = optionalRelations;
            AncestorTraits = ancestorTraits;
        }

        public string ID { get; set; }
        public TraitOriginV1 Origin { get; set; }

        public IImmutableSet<string> AncestorTraits { get; set; }
        public IImmutableList<TraitAttribute> RequiredAttributes { get; set; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; set; }

        public IImmutableList<TraitRelation> OptionalRelations { get; set; }

        public static GenericTrait Build(string id, TraitOriginV1 origin,
            IEnumerable<TraitAttribute> requiredAttributes,
            IEnumerable<TraitAttribute> optionalAttributes,
            IEnumerable<TraitRelation> optionalRelations,
            ISet<string> ancestorTraits)
        {
            return new GenericTrait(id, origin, requiredAttributes.ToImmutableList(), optionalAttributes.ToImmutableList(),
                optionalRelations.ToImmutableList(), ancestorTraits.ToImmutableHashSet());
        }
    }
}
