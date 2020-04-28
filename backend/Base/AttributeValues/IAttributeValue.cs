using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer, JSON
    }

    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public string Value2String();
        public int GetHashCode();
        public AttributeValueDTO ToGeneric();
        public AttributeValueType Type { get; }
        public bool IsArray { get; }

        IEnumerable<ITemplateErrorAttribute> ApplyTextLengthConstraint(int? minimum, int? maximum);
        bool FullTextSearch(string searchString, CompareOptions compareOptions);
        IEnumerable<ITemplateErrorAttribute> MatchRegex(Regex regex);
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
            if (buffer.Length > 0 || input[input.Length - 1] == separator) yield return buffer.Flush();
        }

        public static string Flush(this StringBuilder stringBuilder)
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
                AttributeValueType.Text => AttributeValueTextScalar.Build(value, false),
                AttributeValueType.MultilineText => AttributeValueTextScalar.Build(value, true),
                AttributeValueType.Integer => AttributeValueIntegerScalar.Build(value),
                AttributeValueType.JSON => AttributeValueJSONScalar.Build(value),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }
        private static IAttributeValue BuildArray(AttributeValueType type, string[] values)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeValueTextArray.Build(values, false),
                AttributeValueType.MultilineText => AttributeValueTextArray.Build(values, true),
                AttributeValueType.Integer => AttributeValueIntegerArray.Build(values),
                AttributeValueType.JSON => AttributeValueJSONArray.Build(values),
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
