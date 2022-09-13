using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IPredicateSelection
    {
    }

    public abstract class PredicateSelectionBase : IPredicateSelection
    {
        public abstract override string ToString(); // this is the reason the base class exists: to force subclasses to override ToString(): https://stackoverflow.com/a/510358
    }

    public sealed class PredicateSelectionSpecific : PredicateSelectionBase, IEquatable<PredicateSelectionSpecific>
    {
        public readonly ISet<string> PredicateIDs;

        private PredicateSelectionSpecific(ISet<string> predicateIDs)
        {
            PredicateIDs = predicateIDs;
        }
        public static PredicateSelectionSpecific Build(IEnumerable<string> predicateIDs)
        {
            return new PredicateSelectionSpecific(predicateIDs.ToHashSet());
        }
        public static PredicateSelectionSpecific Build(params string[] predicateIDs)
        {
            return new PredicateSelectionSpecific(predicateIDs.ToHashSet());
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
        public override bool Equals(object? obj) => Equals(obj as PredicateSelectionSpecific);
        public bool Equals(PredicateSelectionSpecific? other) => other != null && PredicateIDs.SetEquals(other.PredicateIDs);

        public override string ToString() => $"PredicateSelectionSpecific:{string.Join(",", PredicateIDs)}";
    }

    public sealed class PredicateSelectionAll : PredicateSelectionBase
    {
        private PredicateSelectionAll() { }
        public static PredicateSelectionAll Instance = new PredicateSelectionAll();
        public override string ToString() => $"PredicateSelectionAll";
    }
    public sealed class PredicateSelectionNone : PredicateSelectionBase
    {
        private PredicateSelectionNone() { }
        public static PredicateSelectionNone Instance = new PredicateSelectionNone();
        public override string ToString() => $"PredicateSelectionNone";
    }

    public static class PredicateSelectionExtensions
    {
        public static IPredicateSelection Union(this IPredicateSelection a, IPredicateSelection other)
        {
            return a switch
            {
                PredicateSelectionAll _ => a,
                PredicateSelectionNone _ => other,
                PredicateSelectionSpecific n => n.Union(other),
                _ => throw new NotImplementedException(),
            };
        }

        public static IPredicateSelection Union(this PredicateSelectionSpecific a, IPredicateSelection other)
        {
            return other switch
            {
                PredicateSelectionAll _ => other,
                PredicateSelectionNone _ => a,
                PredicateSelectionSpecific n => PredicateSelectionSpecific.Build(a.PredicateIDs.Union(n.PredicateIDs).ToHashSet()), // union
                _ => throw new NotImplementedException(),
            };
        }

        public static IPredicateSelection UnionAll(IEnumerable<IPredicateSelection> selections)
        {
            var specific = new HashSet<string>();
            foreach (var selection in selections)
            {
                switch (selection)
                {
                    case PredicateSelectionAll a:
                        return a;
                    case PredicateSelectionSpecific s:
                        specific.UnionWith(s.PredicateIDs);
                        break;
                    case PredicateSelectionNone _:
                        break;
                }
            }
            return PredicateSelectionSpecific.Build(specific);
        }
    }
}
