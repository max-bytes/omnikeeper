using System;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class Predicate : IEquatable<Predicate>
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }
        public AnchorState State { get; private set; }
        public PredicateConstraints Constraints { get; private set; }

        public static Predicate Build(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints)
        {
            return new Predicate
            {
                ID = id,
                WordingFrom = wordingFrom,
                WordingTo = wordingTo,
                State = state,
                Constraints = constraints
            };
        }

        public override bool Equals(object obj) => Equals(obj as Predicate);
        public bool Equals(Predicate other)
        {
            return other != null && ID == other.ID &&
                   WordingFrom == other.WordingFrom &&
                   WordingTo == other.WordingTo &&
                   State == other.State &&
                   Constraints.Equals(other.Constraints);
        }
        public override int GetHashCode() => HashCode.Combine(ID, WordingFrom, WordingTo, State);
    }

    public class DirectedPredicate : IEquatable<DirectedPredicate>
    {
        public string PredicateID { get; private set; }
        public AnchorState PredicateState { get; private set; }
        public string Wording { get; private set; }
        public bool Forward { get; private set; }

        public static DirectedPredicate Build(string predicateID, string wording, AnchorState predicateState, bool forward)
        {
            return new DirectedPredicate
            {
                PredicateID = predicateID,
                PredicateState = predicateState,
                Wording = wording,
                Forward = forward
            };
        }

        public override bool Equals(object obj) => Equals(obj as DirectedPredicate);
        public bool Equals(DirectedPredicate other)
        {
            return other != null && PredicateID == other.PredicateID &&
                   Wording == other.Wording &&
                   PredicateState == other.PredicateState &&
                   Forward == other.Forward;
        }
        public override int GetHashCode() => HashCode.Combine(PredicateID, Wording, PredicateState, Forward);
    }

    public class PredicateConstraints
    {
        public string[] PreferredTraitsTo { get; private set; }
        public string[] PreferredTraitsFrom { get; private set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasPreferredTraitsTo => PreferredTraitsTo.Length > 0;
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasPreferredTraitsFrom => PreferredTraitsFrom.Length > 0;

        private PredicateConstraints() { }
        public PredicateConstraints(string[] preferredTraitsTo, string[] preferredTraitsFrom)
        {
            PreferredTraitsFrom = preferredTraitsFrom;
            PreferredTraitsTo = preferredTraitsTo;
        }

        public static PredicateConstraints Default = new PredicateConstraints(new string[0], new string[0]);

        public override bool Equals(object obj)
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
