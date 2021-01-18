﻿using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity.DTO;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Omnikeeper.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer, JSON, YAML, Image
    }
    [ProtoContract]
    [ProtoInclude(1, typeof(AttributeScalarValueImage))]
    [ProtoInclude(2, typeof(AttributeScalarValueInteger))]
    [ProtoInclude(3, typeof(AttributeScalarValueJSON))]
    [ProtoInclude(4, typeof(AttributeScalarValueText))]
    [ProtoInclude(5, typeof(AttributeScalarValueYAML))]
    [ProtoInclude(51, typeof(AttributeArrayValue<AttributeScalarValueImage, BinaryScalarAttributeValueProxy>))]
    [ProtoInclude(52, typeof(AttributeArrayValue<AttributeScalarValueInteger, long>))]
    [ProtoInclude(53, typeof(AttributeArrayValue<AttributeScalarValueJSON, JToken>))]
    [ProtoInclude(54, typeof(AttributeArrayValue<AttributeScalarValueText, string>))]
    [ProtoInclude(55, typeof(AttributeArrayValue<AttributeScalarValueYAML, YamlDocument>))]
    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public string Value2String();
        public int GetHashCode();
        public object ToGenericObject();
        public AttributeValueType Type { get; }
        public bool IsArray { get; }

        public string[] ToRawDTOValues();
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

    [ProtoContract] // NOTE: cannot skip constructor, because then initializations are not done either, leaving arrays at null
    [ProtoInclude(2, typeof(AttributeArrayValueImage))]
    [ProtoInclude(3, typeof(AttributeArrayValueInteger))]
    [ProtoInclude(4, typeof(AttributeArrayValueJSON))]
    [ProtoInclude(5, typeof(AttributeArrayValueText))]
    [ProtoInclude(6, typeof(AttributeArrayValueYAML))]
    public abstract class AttributeArrayValue<S, T> : IAttributeArrayValue<S, T>, IEquatable<AttributeArrayValue<S, T>> where S : IAttributeScalarValue<T>
    {
        public S[] Values => values;
        [ProtoMember(1)]
        private readonly S[] values = Array.Empty<S>();

        protected AttributeArrayValue(S[] values)
        {
            this.values = values;
        }

#pragma warning disable CS8618
        protected AttributeArrayValue() { }
#pragma warning restore CS8618

        public abstract AttributeValueType Type { get; }

        public bool IsArray => true;

        public override string ToString() => $"AV-Array: {Value2String()}";

        public bool Equals(IAttributeValue? other) => Equals(other as AttributeArrayValue<S, T>);
        public bool Equals(AttributeArrayValue<S, T>? other) => other != null && Values.SequenceEqual(other.Values); // does this work?, or do we have to use zip()?
        public override int GetHashCode() => Values.GetHashCode();

        public string[] ToRawDTOValues() => Values.Select(v => v.ToRawDTOValues()[0]).ToArray();

        public object ToGenericObject() => Values.Select(v => v.Value).ToArray();
        public string Value2String() => string.Join(",", Values.Select(value => value.Value2String().Replace(",", "\\,")));
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
        public static IAttributeValue BuildFromTypeAndObject(AttributeValueType type, object o)
        {
            switch (type)
            {
                case AttributeValueType.Text:
                    {
                        try
                        {
                            if (o.GetType().IsArray)
                                return AttributeArrayValueText.BuildFromString((o as object[]).OfType<string>().ToArray(), false);
                            else
                                return new AttributeScalarValueText((o as string)!, false);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Could not build attribute value of type {type} from object {o}");
                        }
                    }
                case AttributeValueType.MultilineText: 
                    {
                        try
                        {
                            if (o.GetType().IsArray)
                                return AttributeArrayValueText.BuildFromString((o as object[]).OfType<string>().ToArray(), true);
                            else
                                return new AttributeScalarValueText((o as string)!, true);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Could not build attribute value of type {type} from object {o}");
                        }
                    }
                case AttributeValueType.Integer: 
                    {
                        try
                        {
                            if (o.GetType().IsArray)
                                return AttributeArrayValueInteger.Build((o as object[]).OfType<long>().ToArray());
                            else
                                return new AttributeScalarValueInteger((o as long?)!.Value);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Could not build attribute value of type {type} from object {o}");
                        }
                    }
                case AttributeValueType.JSON: 
                    {
                        try
                        {
                            if (o is JArray a)
                                return AttributeArrayValueJSON.Build(a.Children().ToArray());
                            else if (o is object[] oa)
                                return AttributeArrayValueJSON.Build(oa.Select(t => t as JToken)!.ToArray()!);
                            else
                                return AttributeScalarValueJSON.Build((o as JToken)!);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Could not build attribute value of type {type} from object {o}");
                        }
                    }
                case AttributeValueType.YAML: 
                    {
                        try
                        {
                            if (o.GetType().IsArray)
                                return AttributeArrayValueYAML.BuildFromString((o as object[]).OfType<string>().ToArray());
                            else
                                return AttributeScalarValueYAML.BuildFromString((o as string)!);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Could not build attribute value of type {type} from object {o}");
                        }
                    }
                case AttributeValueType.Image: 
                    {
                        throw new Exception("Building AttributeValueImage from type and object not allowed");
                    }
                default:
                    throw new Exception($"Unknown type {type} encountered");
            }
        }
        public static IAttributeValue BuildFromDTO(AttributeValueDTO generic)
        {
            if (generic.IsArray)
                return generic.Type switch
                {
                    AttributeValueType.Text => AttributeArrayValueText.BuildFromString(generic.Values, false),
                    AttributeValueType.MultilineText => AttributeArrayValueText.BuildFromString(generic.Values, true),
                    AttributeValueType.Integer => AttributeArrayValueInteger.BuildFromString(generic.Values),
                    AttributeValueType.JSON => AttributeArrayValueJSON.BuildFromString(generic.Values),
                    AttributeValueType.YAML => AttributeArrayValueYAML.BuildFromString(generic.Values),
                    AttributeValueType.Image => throw new Exception("Building AttributeValueImage from DTO not allowed"),
                    _ => throw new Exception($"Unknown type {generic.Type} encountered"),
                };
            else
                return generic.Type switch
                {
                    AttributeValueType.Text => new AttributeScalarValueText(generic.Values[0], false),
                    AttributeValueType.MultilineText => new AttributeScalarValueText(generic.Values[0], true),
                    AttributeValueType.Integer => AttributeScalarValueInteger.BuildFromString(generic.Values[0]),
                    AttributeValueType.JSON => AttributeScalarValueJSON.BuildFromString(generic.Values[0]),
                    AttributeValueType.YAML => AttributeScalarValueYAML.BuildFromString(generic.Values[0]),
                    AttributeValueType.Image => throw new Exception("Building AttributeValueImage from DTO not allowed"),
                    _ => throw new Exception($"Unknown type {generic.Type} encountered"),
                };
        }
    }
}
