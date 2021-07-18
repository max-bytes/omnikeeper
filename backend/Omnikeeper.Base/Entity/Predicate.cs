using ProtoBuf;
using System;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class Predicate : IEquatable<Predicate>
    {
        public Predicate(string iD, string wordingFrom, string wordingTo, PredicateConstraints constraints)
        {
            ID = iD;
            WordingFrom = wordingFrom;
            WordingTo = wordingTo;
            Constraints = constraints;
        }

        [ProtoMember(1)] public readonly string ID;
        [ProtoMember(2)] public readonly string WordingFrom;
        [ProtoMember(3)] public readonly string WordingTo;
        [ProtoMember(5)] public readonly PredicateConstraints Constraints;

        public override bool Equals(object? obj) => Equals(obj as Predicate);
        public bool Equals(Predicate? other)
        {
            return other != null && ID == other.ID &&
                   WordingFrom == other.WordingFrom &&
                   WordingTo == other.WordingTo &&
                   Constraints.Equals(other.Constraints);
        }
        public override int GetHashCode() => HashCode.Combine(ID, WordingFrom, WordingTo);
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

    [ProtoContract]
    public class PredicateConstraints
    {
        [ProtoMember(1)] public readonly string[] PreferredTraitsTo;
        [ProtoMember(2)] public readonly string[] PreferredTraitsFrom;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasPreferredTraitsTo => PreferredTraitsTo.Length > 0;
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasPreferredTraitsFrom => PreferredTraitsFrom.Length > 0;

        private PredicateConstraints() { PreferredTraitsFrom = new string[0]; PreferredTraitsTo = new string[0]; }
        public PredicateConstraints(string[] preferredTraitsTo, string[] preferredTraitsFrom)
        {
            PreferredTraitsFrom = preferredTraitsFrom;
            PreferredTraitsTo = preferredTraitsTo;
        }

        public static PredicateConstraints Default = new PredicateConstraints(new string[0], new string[0]);

        public override bool Equals(object? obj)
        {
            return obj is PredicateConstraints constraints &&
                   constraints.PreferredTraitsTo != null && PreferredTraitsTo.SequenceEqual(constraints.PreferredTraitsTo) &&
                   constraints.PreferredTraitsFrom != null && PreferredTraitsFrom.SequenceEqual(constraints.PreferredTraitsFrom);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PreferredTraitsTo, PreferredTraitsFrom);
        }
    }
}
