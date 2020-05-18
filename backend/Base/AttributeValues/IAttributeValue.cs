using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer, JSON, YAML
    }

    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public string Value2String();
        public int GetHashCode();
        public AttributeValueDTO ToDTO();
        public object ToGenericObject();
        public AttributeValueType Type { get; }

        IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        bool FullTextSearch(string searchString, CompareOptions compareOptions);
        IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
    }

    public interface IAttributeScalarValue<T> : IAttributeValue
    {
        public T Value { get; }
    }

    public interface IAttributeArrayValue : IAttributeValue
    {

    }

    public interface IAttributeArrayValue<S, T> : IAttributeArrayValue where S : IAttributeScalarValue<T>
    {
        public S[] Values { get; }
    }

    public abstract class AttributeArrayValue<S,T> : IAttributeArrayValue<S, T>, IEquatable<AttributeArrayValue<S,T>> where S : IAttributeScalarValue<T>
    {
        public S[] Values { get; protected set; }

        public abstract AttributeValueType Type { get; }

        public bool IsArray => true;

        public override string ToString() => $"AV-Array: {Value2String()}";

        public bool Equals(IAttributeValue other) => Equals(other as AttributeArrayValue<S,T>);
        public bool Equals(AttributeArrayValue<S,T> other) => other != null && Values.SequenceEqual(other.Values); // does this work?, or do we have to use zip()?
        public override int GetHashCode() => Values.GetHashCode();

        public AttributeValueDTO ToDTO() => AttributeValueDTO.Build(Values.Select(v => v.ToDTO().Values[0]).ToArray(), Type);

        public object ToGenericObject() => Values.Select(v => v.Value).ToArray();
        public string Value2String() => string.Join(",", Values.Select(value => value.Value2String().Replace(",", "\\,")));

        public IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in Values[i].ApplyTextLengthConstraint(minimum, maximum)) yield return e;
            }
        }

        public bool FullTextSearch(string searchString, CompareOptions compareOptions)
        {
            return Values.Any(v => v.FullTextSearch(searchString, compareOptions));
        }

        public IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                foreach (var e in Values[i].MatchRegex(regex)) yield return e;
            }
        }
    }

    public static class Extensions
    {
        public static IEnumerable<string> Tokenize(this string input, char separator, char escape)
        {
            if (input == null) yield break;
            var buffer = new StringBuilder();
            bool escaping = false;
            foreach (char c in input)
            {
                if (escaping)
                {
                    buffer.Append(c);
                    escaping = false;
                }
                else if (c == escape)
                {
                    escaping = true;
                }
                else if (c == separator)
                {
                    yield return buffer.Flush();
                }
                else
                {
                    buffer.Append(c);
                }
            }
            if (input.Length == 0) yield return input;
            else if (buffer.Length > 0 || input[^1] == separator) yield return buffer.Flush();
        }

        private static string Flush(this StringBuilder stringBuilder)
        {
            string result = stringBuilder.ToString();
            stringBuilder.Clear();
            return result;
        }
    }

    public static class AttributeValueBuilder
    {
        public static IAttributeValue Build(AttributeValueDTO generic)
        {
            if (generic.IsArray)
                return BuildArray(generic.Type, generic.Values);
            else
                return BuildScalar(generic.Type, generic.Values[0]);
        }
        private static IAttributeValue BuildScalar(AttributeValueType type, string value)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeScalarValueText.Build(value, false),
                AttributeValueType.MultilineText => AttributeScalarValueText.Build(value, true),
                AttributeValueType.Integer => AttributeValueIntegerScalar.Build(value),
                AttributeValueType.JSON => AttributeScalarValueJSON.Build(value),
                AttributeValueType.YAML => AttributeScalarValueYAML.Build(value),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }
        private static IAttributeValue BuildArray(AttributeValueType type, string[] values)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeArrayValueText.Build(values, false),
                AttributeValueType.MultilineText => AttributeArrayValueText.Build(values, true),
                AttributeValueType.Integer => AttributeValueIntegerArray.Build(values),
                AttributeValueType.JSON => AttributeArrayValueJSON.Build(values),
                AttributeValueType.YAML => AttributeArrayValueYAML.Build(values),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }

        public static IAttributeValue BuildFromDatabase(string value, AttributeValueType type)
        {
            var generic = AttributeValueDTO.BuildFromDatabase(value, type);
            return Build(generic);
        }
    }
}
