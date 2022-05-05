using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IRelationSelection
    {
    }
    public class RelationSelectionFrom : IRelationSelection, IEquatable<RelationSelectionFrom>
    {
        public ISet<Guid> FromCIIDs { get; }
        public ISet<string>? PredicateIDs { get; } // NOTE: null means all

        private RelationSelectionFrom(ISet<Guid> fromCIIDs, ISet<string>? predicateIDs)
        {
            FromCIIDs = fromCIIDs;
            PredicateIDs = predicateIDs;
        }

        public static IRelationSelection BuildWithAllPredicateIDs(ISet<Guid> fromCIIDs)
        {
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs, null);
        }
        public static IRelationSelection BuildWithAllPredicateIDs(params Guid[] fromCIIDs)
        {
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs.ToHashSet(), null);
        }

        public static IRelationSelection Build(ISet<string> predicateIDs, ISet<Guid> fromCIIDs)
        {
            if (predicateIDs.IsEmpty()) return RelationSelectionNone.Instance;
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs, predicateIDs);
        }
        public static IRelationSelection Build(ISet<string> predicateIDs, params Guid[] fromCIIDs)
        {
            if (predicateIDs.IsEmpty()) return RelationSelectionNone.Instance;
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs.ToHashSet(), predicateIDs);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var ciid in FromCIIDs)
                    hash = (hash * 16777619) ^ ciid.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as RelationSelectionFrom);
        public bool Equals(RelationSelectionFrom? other) => other != null && FromCIIDs.SetEquals(other.FromCIIDs);
    }
    public class RelationSelectionTo : IRelationSelection, IEquatable<RelationSelectionTo>
    {
        public ISet<Guid> ToCIIDs { get; }
        public ISet<string>? PredicateIDs { get; } // NOTE: null means all
        private RelationSelectionTo(ISet<Guid> toCIIDs, ISet<string>? predicateIDs)
        {
            ToCIIDs = toCIIDs;
            PredicateIDs = predicateIDs;
        }

        public static IRelationSelection BuildWithAllPredicateIDs(ISet<Guid> toCIIDs)
        {
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs, null);
        }
        public static IRelationSelection BuildWithAllPredicateIDs(params Guid[] toCIIDs)
        {
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs.ToHashSet(), null);
        }
        public static IRelationSelection Build(ISet<string> predicateIDs, ISet<Guid> toCIIDs)
        {
            if (predicateIDs.IsEmpty()) return RelationSelectionNone.Instance;
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs, predicateIDs);
        }
        public static IRelationSelection Build(ISet<string> predicateIDs, params Guid[] toCIIDs)
        {
            if (predicateIDs.IsEmpty()) return RelationSelectionNone.Instance;
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs.ToHashSet(), predicateIDs);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var ciid in ToCIIDs)
                    hash = (hash * 16777619) ^ ciid.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as RelationSelectionTo);
        public bool Equals(RelationSelectionTo? other) => other != null && ToCIIDs.SetEquals(other.ToCIIDs);
    }
    public class RelationSelectionWithPredicate : IRelationSelection, IEquatable<RelationSelectionWithPredicate>
    {
        public readonly ISet<string> PredicateIDs;

        private RelationSelectionWithPredicate(ISet<string> predicateIDs)
        {
            PredicateIDs = predicateIDs;
        }
        public static RelationSelectionWithPredicate Build(IEnumerable<string> predicateIDs)
        {
            return new RelationSelectionWithPredicate(predicateIDs.ToHashSet());
        }
        public static RelationSelectionWithPredicate Build(params string[] predicateIDs)
        {
            return new RelationSelectionWithPredicate(predicateIDs.ToHashSet());
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var predicateID in PredicateIDs)
                    hash = (hash * 16777619) ^ predicateID.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as RelationSelectionWithPredicate);
        public bool Equals(RelationSelectionWithPredicate? other) => other != null && PredicateIDs.SetEquals(other.PredicateIDs);
    }

    public class RelationSelectionSpecific : IRelationSelection, IEquatable<RelationSelectionSpecific>
    {
        public ISet<(Guid from, Guid to, string predicateID)> Specifics { get; }
        private RelationSelectionSpecific(ISet<(Guid from, Guid to, string predicateID)> specifics)
        {
            Specifics = specifics;
        }

        public static IRelationSelection Build(IEnumerable<(Guid from, Guid to, string predicateID)> from)
        {
            if (from.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionSpecific(from.ToHashSet());
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136265;
                foreach (var t in Specifics)
                    hash = (hash * 16777619) ^ t.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as RelationSelectionSpecific);
        public bool Equals(RelationSelectionSpecific? other) => other != null && Specifics.SetEquals(other.Specifics);
    }

    public class RelationSelectionAll : IRelationSelection
    {
        private RelationSelectionAll() { }
        public static RelationSelectionAll Instance = new RelationSelectionAll();
    }
    public class RelationSelectionNone : IRelationSelection
    {
        private RelationSelectionNone() { }
        public static RelationSelectionNone Instance = new RelationSelectionNone();
    }
}
