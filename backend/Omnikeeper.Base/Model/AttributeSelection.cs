using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Base.Model
{
    public interface IAttributeSelection
    {
        bool Contains(string attributeName);
    }

    public class RegexAttributeSelection : IAttributeSelection, IEquatable<RegexAttributeSelection>
    {
        public readonly string RegexStr;
        public readonly Regex RegexCompiled;

        public RegexAttributeSelection(string regex) {
            RegexCompiled = new Regex(regex);
            RegexStr = regex;
        }

        public bool Contains(string attributeName) => RegexCompiled.IsMatch(attributeName);
        public override int GetHashCode() => RegexStr.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as RegexAttributeSelection);
        public bool Equals(RegexAttributeSelection? other) => other != null && RegexStr == other.RegexStr;
    }

    public class NamedAttributesSelection : IAttributeSelection, IEquatable<NamedAttributesSelection>
    {
        public readonly ISet<string> AttributeNames;

        private NamedAttributesSelection(ISet<string> attributeNames)
        {
            AttributeNames = attributeNames;
        }

        public static NamedAttributesSelection Build(params string[] attributeNames)
        {
            return new NamedAttributesSelection(attributeNames.ToHashSet());
        }
        public static IAttributeSelection Build(ISet<string> attributeNames)
        {
            return new NamedAttributesSelection(attributeNames);
        }

        public bool Contains(string attributeName) => AttributeNames.Contains(attributeName);
        public override int GetHashCode() => AttributeNames.GetHashCode();
        public override bool Equals(object? obj) => Equals(obj as NamedAttributesSelection);
        public bool Equals(NamedAttributesSelection? other) => other != null && AttributeNames.SetEquals(other.AttributeNames);

    }

    public class AllAttributeSelection : IAttributeSelection, IEquatable<AllAttributeSelection>
    {
        private AllAttributeSelection() { }

        public static AllAttributeSelection Instance = new AllAttributeSelection();

        public bool Contains(string attributeName) => true;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as AllAttributeSelection);
        public bool Equals(AllAttributeSelection? other) => other != null;
    }
}
