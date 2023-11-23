using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public interface ITrait
    {
        public string ID { get; }
        public string[] AncestorTraits { get; }
        public TraitOriginV1 Origin { get; }

        public TraitAttribute[] RequiredAttributes { get; }
        public TraitAttribute[] OptionalAttributes { get; }

        public TraitRelation[] OptionalRelations { get; }
    }

    public static class TraitExtensions
    {
        public static IReadOnlySet<string> GetRelevantAttributeNames(this ITrait trait)
        {
            return trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Concat(trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet();
        }
        public static IReadOnlySet<string> GetRelevantPredicateIDs(this ITrait trait)
        {
            return trait.OptionalRelations.Select(r => r.RelationTemplate.PredicateID).ToHashSet();
        }
    }

    /// <summary>
    /// comparable to RecursiveTrait, but with the recursive dependencies resolved and flattened
    /// </summary>
    public class GenericTrait : ITrait
    {
        private GenericTrait(string id, TraitOriginV1 origin, TraitAttribute[] requiredAttributes, TraitAttribute[] optionalAttributes,
            TraitRelation[] optionalRelations, string[] ancestorTraits)
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

        public string[] AncestorTraits { get; set; }

        public TraitAttribute[] RequiredAttributes { get; set; }
        public TraitAttribute[] OptionalAttributes { get; set; }

        public TraitRelation[] OptionalRelations { get; set; }

        public static GenericTrait Build(string id, TraitOriginV1 origin,
            IEnumerable<TraitAttribute> requiredAttributes,
            IEnumerable<TraitAttribute> optionalAttributes,
            IEnumerable<TraitRelation> optionalRelations,
            ISet<string> ancestorTraits)
        {
            return new GenericTrait(id, origin, requiredAttributes.ToArray(), optionalAttributes.ToArray(),
                optionalRelations.ToArray(), ancestorTraits.ToArray());
        }
    }
}
