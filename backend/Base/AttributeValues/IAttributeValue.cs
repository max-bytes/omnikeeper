using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LandscapeRegistry.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer
    }

    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public string Value2String();
        public int GetHashCode();
        public AttributeValueGeneric ToGeneric();
        public AttributeValueType Type { get; }
        public bool IsArray { get; }
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


    public class AttributeValueGeneric : IEquatable<AttributeValueGeneric>
    {
        public AttributeValueType Type { get; protected set; }
        public bool IsArray { get; private set; }
        public string[] Values { get; private set; }
        public string Value => Values[0];

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueGeneric);
        public bool Equals([AllowNull] AttributeValueGeneric other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => HashCode.Combine(IsArray, Values.GetHashCode());

        public static AttributeValueGeneric Build(string value, AttributeValueType type)
        {
            return new AttributeValueGeneric()
            {
                Values = new string[] { value },
                IsArray = false,
                Type = type
            };
        }
        public static AttributeValueGeneric Build(string[] values, AttributeValueType type)
        {
            return new AttributeValueGeneric()
            {
                Values = values,
                IsArray = true,
                Type = type
            };
        }

        public string Value2DatabaseString() {
            if (IsArray)
            {
                return $"A{MarshalValues(Values)}";
            } else
            {
                return $"S{Value}";
            }
            
        }

        public static AttributeValueGeneric BuildFromDatabase(string value, AttributeValueType type)
        {
            var multiplicityIndicator = value.Substring(0, 1);
            var finalValue = value.Substring(1);
            if (multiplicityIndicator == "A")
            {
                var finalValues = UnmarshalValues(finalValue);
                return Build(finalValues, type);
            }
            else
                return Build(finalValue, type);
        }
        public static string MarshalValues(string[] values)
        {
            return string.Join(",", values.Select(value => value.Replace("\\", "\\\\").Replace(",", "\\,")));
        }
        public static string[] UnmarshalValues(string value)
        {
            var values = value.Tokenize(',', '\\');
            return values.Select(v => v.Replace("\\\\", "\\")).ToArray();
        }
    }

    public static class AttributeValueBuilder
    {
        public static IAttributeValue Build(AttributeValueGeneric generic)
        {
            if (generic.IsArray)
                return BuildArray(generic.Type, generic.Values);
            else
                return BuildScalar(generic.Type, generic.Value);
        }
        public static IAttributeValue BuildScalar(AttributeValueType type, string value)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeValueTextScalar.Build(value, false),
                AttributeValueType.MultilineText => AttributeValueTextScalar.Build(value, true),
                AttributeValueType.Integer => AttributeValueIntegerScalar.Build(value),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }
        public static IAttributeValue BuildArray(AttributeValueType type, string[] values)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeValueTextArray.Build(values, false),
                AttributeValueType.MultilineText => AttributeValueTextArray.Build(values, true),
                AttributeValueType.Integer => AttributeValueIntegerArray.Build(values),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }

        public static IAttributeValue BuildFromDatabase(string value, AttributeValueType type)
        {
            var generic = AttributeValueGeneric.BuildFromDatabase(value, type);
            return Build(generic);
        }
    }
}
