using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omnikeeper.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer, JSON, YAML, Image, Mask, Double, Boolean, DateTimeWithOffset
    }
    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public string Value2String();
        public int GetHashCode();
        public object ToGenericObject();
        public AttributeValueType Type { get; }
        public bool IsArray { get; }

        public string[] ToRawDTOValues();

        public object ToGraphQLValue();
    }

    public interface IAttributeScalarValue<T> : IAttributeValue
    {
        public T Value { get; }
    }

    public interface IAttributeArrayValue : IAttributeValue
    {
        public int Length { get; }
    }

    public interface IAttributeArrayValue<S, T> : IAttributeArrayValue where S : IAttributeScalarValue<T>
    {
        public S[] Values { get; }
    }

    public abstract record class AttributeArrayValue<S, T>(S[] Values) : IAttributeArrayValue<S, T>, IEquatable<AttributeArrayValue<S, T>> where S : IAttributeScalarValue<T>
    {

        public abstract AttributeValueType Type { get; }

        public int Length => Values.Length;

        public bool IsArray => true;

        public override string ToString() => $"AV-Array: {Value2String()}";

        public bool Equals(IAttributeValue? other) => Equals(other as AttributeArrayValue<S, T>);
        public virtual bool Equals(AttributeArrayValue<S, T>? other) => other != null && Values.SequenceEqual(other.Values); // does this work?, or do we have to use zip()?
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values.Select(v => v.ToRawDTOValues()[0]).ToArray();

        public object ToGenericObject() => Values.Select(v => v.Value).ToArray();
        public string Value2String() => string.Join(",", Values.Select(value => value.Value2String().Replace(",", "\\,")));

        public object ToGraphQLValue() => Values.Select(v => v.ToGraphQLValue()).ToArray();
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


}
