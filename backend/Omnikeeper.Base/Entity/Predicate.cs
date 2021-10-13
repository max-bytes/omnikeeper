using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.predicate", TraitOriginType.Core)]
    public class Predicate : TraitEntity, IEquatable<Predicate>
    {
        public Predicate()
        {
            ID = "";
            WordingFrom = "";
            WordingTo = "";
            Name = "";
        }

        public Predicate(string iD, string wordingFrom, string wordingTo)
        {
            ID = iD;
            WordingFrom = wordingFrom;
            WordingTo = wordingTo;
            Name = $"Predicate - {ID}";
        }

        [TraitAttribute("id", "predicate.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.PredicateIDRegexString, IDValidations.PredicateIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("wording_from", "predicate.wording_from")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string WordingFrom;

        [TraitAttribute("wording_to", "predicate.wording_to")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string WordingTo;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public override bool Equals(object? obj) => Equals(obj as Predicate);
        public bool Equals(Predicate? other)
        {
            return other != null && ID == other.ID &&
                   WordingFrom == other.WordingFrom &&
                   WordingTo == other.WordingTo && 
                   Name == other.Name;
        }
        public override int GetHashCode() => HashCode.Combine(ID, WordingFrom, WordingTo, Name);
    }

    public class DirectedPredicate : IEquatable<DirectedPredicate>
    {
        public DirectedPredicate(string predicateID, string wording, bool forward)
        {
            PredicateID = predicateID;
            Wording = wording;
            Forward = forward;
        }

        public readonly string PredicateID;
        public readonly string Wording;
        public readonly bool Forward;

        public override bool Equals(object? obj) => Equals(obj as DirectedPredicate);
        public bool Equals(DirectedPredicate? other)
        {
            return other != null && PredicateID == other.PredicateID &&
                   Wording == other.Wording &&
                   Forward == other.Forward;
        }
        public override int GetHashCode() => HashCode.Combine(PredicateID, Wording, Forward);
    }
}
