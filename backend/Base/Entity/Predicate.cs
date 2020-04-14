
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public enum PredicateState
    {
        Active, Deprecated, Inactive
    }

    public class Predicate : IEquatable<Predicate>
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }
        public PredicateState State { get; private set; }

        public static Predicate Build(string id, string wordingFrom, string wordingTo, PredicateState state)
        {
            return new Predicate
            {
                ID = id,
                WordingFrom = wordingFrom,
                WordingTo = wordingTo,
                State = state
            };
        }

        public override bool Equals(object obj) => Equals(obj as Predicate);
        public bool Equals(Predicate other)
        {
            return ID == other.ID &&
                   WordingFrom == other.WordingFrom &&
                   WordingTo == other.WordingTo &&
                   State == other.State;
        }
        public override int GetHashCode() => HashCode.Combine(ID, WordingFrom, WordingTo, State);
    }
}
