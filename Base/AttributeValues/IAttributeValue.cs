using System;

namespace LandscapePrototype.Entity.AttributeValues
{

    public enum AttributeValueType
    {
        Text, Integer
    }

    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public abstract string Value2String();
        public abstract int GetHashCode();
    }

    public class AttributeValueGeneric
    {
        public AttributeValueType Type { get; private set; }
        public string Value { get; private set; }
    }

    public static class AttributeValueBuilder
    {
        public static IAttributeValue Build(AttributeValueGeneric generic)
        {
            return Build(generic.Type, generic.Value);
        }
        public static IAttributeValue Build(AttributeValueType type, string value)
        {
            return type switch
            {
                AttributeValueType.Text => AttributeValueText.Build(value),
                AttributeValueType.Integer => AttributeValueInteger.Build(value),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }

        public static AttributeValueType GetType(IAttributeValue av)
        {
            return av switch
            {
                AttributeValueText _ => AttributeValueType.Text,
                AttributeValueInteger _ => AttributeValueType.Integer,
                _ => throw new Exception($"Unknown AttributeValue {av} encountered"),
            };
        }

        public static (AttributeValueType, string) GetTypeAndValueString(IAttributeValue av)
        {
            return (GetType(av), av.Value2String());
        }
    }
}
