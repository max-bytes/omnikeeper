using System;

namespace LandscapePrototype.Entity.AttributeValues
{
    public interface IAttributeValue : IEquatable<IAttributeValue>
    {
        public abstract string Value2String();
        public abstract int GetHashCode();
    }

    public class AttributeValueGeneric
    {
        public string Type { get; private set; }
        public string Value { get; private set; }
    }

    public static class AttributeValueBuilder
    {
        public static IAttributeValue Build(AttributeValueGeneric generic)
        {
            return Build(generic.Type, generic.Value);
        }
        public static IAttributeValue Build(string type, string value)
        {
            return type switch
            {
                "text" => AttributeValueText.Build(value),
                "integer" => AttributeValueInteger.Build(value),
                _ => throw new Exception($"Unknown type {type} encountered"),
            };
        }

        public static string GetTypeString(IAttributeValue av)
        {
            return av switch
            {
                AttributeValueText _ => "text",
                AttributeValueInteger _ => "integer",
                _ => throw new Exception($"Unknown AttributeValue {av} encountered"),
            };
        }

        public static (string, string) GetTypeAndValueString(IAttributeValue av)
        {
            return (GetTypeString(av), av.Value2String());
        }
    }
}
