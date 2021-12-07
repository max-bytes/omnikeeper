using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface ITraitSelection
    {
        bool Contains(string traitID);
    }

    public class NamedTraitsSelection : ITraitSelection, IEquatable<NamedTraitsSelection>
    {
        public readonly ISet<string> TraitIDs;

        private NamedTraitsSelection(ISet<string> traitIDs)
        {
            TraitIDs = traitIDs;
        }

        public static ITraitSelection Build(params string[] traitIDs)
        {
            if (traitIDs.IsEmpty())
                return NoTraitsSelection.Instance;
            return new NamedTraitsSelection(traitIDs.ToHashSet());
        }
        public static ITraitSelection Build(ISet<string> traitIDs)
        {
            if (traitIDs.IsEmpty())
                return NoTraitsSelection.Instance;
            return new NamedTraitsSelection(traitIDs);
        }

        public bool Contains(string traitID) => TraitIDs.Contains(traitID);
        public override int GetHashCode() => TraitIDs.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as NamedTraitsSelection);
        public bool Equals(NamedTraitsSelection? other) => other != null && TraitIDs.SetEquals(other.TraitIDs);

    }

    public class AllTraitsSelection : ITraitSelection, IEquatable<AllTraitsSelection>
    {
        private AllTraitsSelection() { }

        public static AllTraitsSelection Instance = new AllTraitsSelection();

        public bool Contains(string traitID) => true;

        public override int GetHashCode() => 1;
        public override bool Equals(object? obj) => Equals(obj as AllTraitsSelection);
        public bool Equals(AllTraitsSelection? other) => other != null;
    }

    public class NoTraitsSelection : ITraitSelection, IEquatable<NoTraitsSelection>
    {
        private NoTraitsSelection() { }

        public static NoTraitsSelection Instance = new NoTraitsSelection();

        public bool Contains(string traitID) => false;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as NoTraitsSelection);
        public bool Equals(NoTraitsSelection? other) => other != null;
    }

    public static class TraitSelectionExtensions
    {
        public static ITraitSelection Union(this ITraitSelection a, ITraitSelection other)
        {
            return a switch
            {
                AllTraitsSelection _ => a,
                NoTraitsSelection _ => other,
                NamedTraitsSelection n => n.Union(other),
                _ => throw new NotImplementedException(),
            };
        }

        public static ITraitSelection Union(this NamedTraitsSelection a, ITraitSelection other)
        {
            return other switch
            {
                AllTraitsSelection _ => other,
                NoTraitsSelection _ => a,
                NamedTraitsSelection n => NamedTraitsSelection.Build(a.TraitIDs.Union(n.TraitIDs).ToHashSet()), // union
                _ => throw new NotImplementedException(),
            };
        }

        public static ITraitSelection UnionAll(IEnumerable<ITraitSelection> selections)
        {
            var specific = new HashSet<string>();
            foreach (var selection in selections)
            {
                switch (selection)
                {
                    case AllTraitsSelection a:
                        return a;
                    case NamedTraitsSelection s:
                        specific.UnionWith(s.TraitIDs);
                        break;
                    case NoTraitsSelection _:
                        break;
                }
            }
            return NamedTraitsSelection.Build(specific);
        }
    }
}
