using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IAttributeSelection
    {
        bool ContainsAttributeName(string attributeName);
        bool ContainsAttribute(CIAttribute attribute);
    }

    public abstract class AttributeSelectionBase : IAttributeSelection
    {
        public abstract bool ContainsAttribute(CIAttribute attribute);
        public abstract bool ContainsAttributeName(string attributeName);
        public abstract override string ToString(); // this is the reason the base class exists: to force subclasses to override ToString(): https://stackoverflow.com/a/510358
    }

    public sealed class NamedAttributesSelection : AttributeSelectionBase, IEquatable<NamedAttributesSelection>
    {
        public readonly IReadOnlySet<string> AttributeNames;

        private NamedAttributesSelection(IReadOnlySet<string> attributeNames)
        {
            AttributeNames = attributeNames;
        }

        public static IAttributeSelection Build(params string[] attributeNames)
        {
            if (attributeNames.IsEmpty())
                return NoAttributesSelection.Instance;
            return new NamedAttributesSelection(attributeNames.ToImmutableHashSet());
        }
        public static IAttributeSelection Build(IReadOnlySet<string> attributeNames)
        {
            if (attributeNames.IsEmpty())
                return NoAttributesSelection.Instance;
            return new NamedAttributesSelection(attributeNames);
        }

        public override bool ContainsAttributeName(string attributeName) => AttributeNames.Contains(attributeName);
        public override bool ContainsAttribute(CIAttribute attribute) => AttributeNames.Contains(attribute.Name);
        public override int GetHashCode() => AttributeNames.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as NamedAttributesSelection);
        public bool Equals(NamedAttributesSelection? other) => other != null && AttributeNames.SetEquals(other.AttributeNames);

        public override string ToString() => $"NamedAttributeSelection:{string.Join(",", AttributeNames)}";
    }

    public sealed class AllAttributeSelection : AttributeSelectionBase, IEquatable<AllAttributeSelection>
    {
        private AllAttributeSelection() { }

        public static AllAttributeSelection Instance = new AllAttributeSelection();

        public override bool ContainsAttributeName(string attributeName) => true;
        public override bool ContainsAttribute(CIAttribute attribute) => true;

        public override int GetHashCode() => 1;
        public override bool Equals(object? obj) => Equals(obj as AllAttributeSelection);
        public bool Equals(AllAttributeSelection? other) => other != null;

        public override string ToString() => $"AllAttributeSelection";
    }

    public sealed class NoAttributesSelection : AttributeSelectionBase, IEquatable<NoAttributesSelection>
    {
        private NoAttributesSelection() { }

        public static NoAttributesSelection Instance = new NoAttributesSelection();

        public override bool ContainsAttributeName(string attributeName) => false;
        public override bool ContainsAttribute(CIAttribute attribute) => false;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as NoAttributesSelection);
        public bool Equals(NoAttributesSelection? other) => other != null;

        public override string ToString() => $"NoAttributesSelection";
    }

    public static class AttributeSelectionExtensions
    {
        public static IAttributeSelection Union(this IAttributeSelection a, IAttributeSelection other)
        {
            return a switch
            {
                AllAttributeSelection _ => a,
                NoAttributesSelection _ => other,
                NamedAttributesSelection n => n.Union(other),
                _ => throw new NotImplementedException(),
            };
        }

        public static IAttributeSelection Union(this NamedAttributesSelection a, IAttributeSelection other)
        {
            return other switch
            {
                AllAttributeSelection _ => other,
                NoAttributesSelection _ => a,
                NamedAttributesSelection n => NamedAttributesSelection.Build(a.AttributeNames.Union(n.AttributeNames).ToHashSet()), // union
                _ => throw new NotImplementedException(),
            };
        }

        public static IAttributeSelection UnionAll(IEnumerable<IAttributeSelection> selections)
        {
            var specific = new HashSet<string>();
            foreach (var selection in selections)
            {
                switch (selection)
                {
                    case AllAttributeSelection a:
                        return a;
                    case NamedAttributesSelection s:
                        specific.UnionWith(s.AttributeNames);
                        break;
                    case NoAttributesSelection _:
                        break;
                }
            }
            return NamedAttributesSelection.Build(specific);
        }
    }
}
