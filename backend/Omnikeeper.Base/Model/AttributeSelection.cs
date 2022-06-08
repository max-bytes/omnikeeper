using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
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

    public class NamedAttributesSelection : IAttributeSelection, IEquatable<NamedAttributesSelection>
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

        public bool ContainsAttributeName(string attributeName) => AttributeNames.Contains(attributeName);
        public bool ContainsAttribute(CIAttribute attribute) => AttributeNames.Contains(attribute.Name);
        public override int GetHashCode() => AttributeNames.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as NamedAttributesSelection);
        public bool Equals(NamedAttributesSelection? other) => other != null && AttributeNames.SetEquals(other.AttributeNames);
    }

    //public class NamedAttributesWithValueFiltersSelection : IAttributeSelection, IEquatable<NamedAttributesWithValueFiltersSelection>
    //{
    //    // NOTE: keys are attributeNames
    //    public readonly IDictionary<string, AttributeScalarTextFilter> NamesAndFilters;

    //    private NamedAttributesWithValueFiltersSelection(IDictionary<string, AttributeScalarTextFilter> namesAndFilters)
    //    {
    //        NamesAndFilters = namesAndFilters;
    //    }

    //    public static IAttributeSelection Build(IDictionary<string, AttributeScalarTextFilter> namesAndFilters)
    //    {
    //        if (namesAndFilters.IsEmpty())
    //            return NoAttributesSelection.Instance;
    //        return new NamedAttributesWithValueFiltersSelection(namesAndFilters);
    //    }

    //    public bool ContainsAttributeName(string attributeName) => NamesAndFilters.ContainsKey(attributeName);
    //    public bool ContainsAttribute(CIAttribute attribute)
    //    {
    //        if (!NamesAndFilters.TryGetValue(attribute.Name, out var filter))
    //            return false;

    //        return filter.Contains(attribute.Value);
    //    }
    //    public override int GetHashCode() => NamesAndFilters.GetHashCode();
    //    public override bool Equals(object? obj) => Equals(obj as NamedAttributesWithValueFiltersSelection);
    //    public bool Equals(NamedAttributesWithValueFiltersSelection? other) => other != null && NamesAndFilters.SequenceEqual(other.NamesAndFilters);
    //}

    public class AllAttributeSelection : IAttributeSelection, IEquatable<AllAttributeSelection>
    {
        private AllAttributeSelection() { }

        public static AllAttributeSelection Instance = new AllAttributeSelection();

        public bool ContainsAttributeName(string attributeName) => true;
        public bool ContainsAttribute(CIAttribute attribute) => true;

        public override int GetHashCode() => 1;
        public override bool Equals(object? obj) => Equals(obj as AllAttributeSelection);
        public bool Equals(AllAttributeSelection? other) => other != null;
    }

    public class NoAttributesSelection : IAttributeSelection, IEquatable<NoAttributesSelection>
    {
        private NoAttributesSelection() { }

        public static NoAttributesSelection Instance = new NoAttributesSelection();

        public bool ContainsAttributeName(string attributeName) => false;
        public bool ContainsAttribute(CIAttribute attribute) => false;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as NoAttributesSelection);
        public bool Equals(NoAttributesSelection? other) => other != null;
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
                //NamedAttributesWithValueFiltersSelection _ => throw new NotImplementedException(),
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
                //NamedAttributesWithValueFiltersSelection _ => throw new NotImplementedException(),
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
                    //case NamedAttributesWithValueFiltersSelection _:
                    //    throw new NotImplementedException();
                    case NoAttributesSelection _:
                        break;
                }
            }
            return NamedAttributesSelection.Build(specific);
        }
    }
}
