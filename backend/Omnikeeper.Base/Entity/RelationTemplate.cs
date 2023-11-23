using System;
using System.Collections;

namespace Omnikeeper.Base.Entity
{
    public class RelationTemplate : IEquatable<RelationTemplate>
    {
        public readonly string PredicateID;
        public readonly bool DirectionForward;

        public readonly string[] TraitHints;

        public RelationTemplate(string predicateID, bool directionForward, string[]? traitHints = null)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            TraitHints = traitHints ?? Array.Empty<string>();
        }

        public bool Equals(RelationTemplate? other)
        {
            // NOTE: see https://stackoverflow.com/questions/69133392/computing-hashcode-of-combination-of-value-type-and-array why we use StruturalComparisons
            return other != null && PredicateID == other.PredicateID && DirectionForward == other.DirectionForward && StructuralComparisons.StructuralEqualityComparer.Equals(TraitHints, other.TraitHints);
        }
        public override bool Equals(object? other) => Equals(other as RelationTemplate);
        public override int GetHashCode() => HashCode.Combine(PredicateID, DirectionForward, StructuralComparisons.StructuralEqualityComparer.GetHashCode(TraitHints));
    }
}
