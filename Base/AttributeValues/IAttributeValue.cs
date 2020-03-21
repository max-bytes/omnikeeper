using System;

namespace LandscapePrototype.Entity.AttributeValues
{
    public enum AttributeValueType
    {
        Text, MultilineText, Integer
    }

    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public abstract string Value2String();
        public abstract int GetHashCode();
        public abstract AttributeValueGeneric ToGeneric();
        public AttributeValueType Type { get; }
    }

    public class AttributeValueGeneric
    {
        public AttributeValueType Type { get; private set; }
        public string Value { get; private set; }

        public static AttributeValueGeneric Build(string value, AttributeValueType type)
        {
            return new AttributeValueGeneric()
            {
                Value = value,
                Type = type
            };
        }
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
                AttributeValueType.Text => AttributeValueText.Build(value, false),
                AttributeValueType.Integer => AttributeValueInteger.Build(value),
                AttributeValueType.MultilineText => AttributeValueText.Build(value, true),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }

        //public static AttributeValueType GetType(IAttributeValue av)
        //{
        //    return av switch
        //    {
        //        AttributeValueText t => (t.Multiline) ? AttributeValueType.MultilineText : AttributeValueType.Text,
        //        AttributeValueInteger _ => AttributeValueType.Integer,
        //        _ => throw new Exception($"Unknown AttributeValue {av} encountered"),
        //    };
        //}

        //public static (AttributeValueType, string) GetTypeAndValueString(IAttributeValue av)
        //{
        //    return (av.Type, av.Value2String());
        //}
    }
}
