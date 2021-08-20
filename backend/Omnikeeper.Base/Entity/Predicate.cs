using ProtoBuf;
using System;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class Predicate : IEquatable<Predicate>
    {
        public Predicate(string iD, string wordingFrom, string wordingTo)
        {
            ID = iD;
            WordingFrom = wordingFrom;
            WordingTo = wordingTo;
        }

        [ProtoMember(1)] public readonly string ID;
        [ProtoMember(2)] public readonly string WordingFrom;
        [ProtoMember(3)] public readonly string WordingTo;

        public override bool Equals(object? obj) => Equals(obj as Predicate);
        public bool Equals(Predicate? other)
        {
            return other != null && ID == other.ID &&
                   WordingFrom == other.WordingFrom &&
                   WordingTo == other.WordingTo;
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
}
