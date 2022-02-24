﻿using Omnikeeper.Base.Utils;
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
        private RelationSelectionFrom(ISet<Guid> fromCIIDs)
        {
            FromCIIDs = fromCIIDs;
        }

        public static IRelationSelection Build(ISet<Guid> fromCIIDs)
        {
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs);
        }
        public static IRelationSelection Build(params Guid[] fromCIIDs)
        {
            if (fromCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionFrom(fromCIIDs.ToHashSet());
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
        private RelationSelectionTo(ISet<Guid> toCIIDs)
        {
            ToCIIDs = toCIIDs;
        }

        public static IRelationSelection Build(ISet<Guid> toCIIDs)
        {
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs);
        }
        public static IRelationSelection Build(params Guid[] toCIIDs)
        {
            if (toCIIDs.IsEmpty()) return RelationSelectionNone.Instance;
            return new RelationSelectionTo(toCIIDs.ToHashSet());
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
        public readonly string PredicateID;

        private RelationSelectionWithPredicate(string predicateID)
        {
            PredicateID = predicateID;
        }
        public static RelationSelectionWithPredicate Build(string predicateID)
        {
            return new RelationSelectionWithPredicate(predicateID);
        }

        public override int GetHashCode()
        {
            return PredicateID.GetHashCode();
        }
        public override bool Equals(object? obj) => Equals(obj as RelationSelectionWithPredicate);
        public bool Equals(RelationSelectionWithPredicate? other) => other != null && PredicateID == other.PredicateID;
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
