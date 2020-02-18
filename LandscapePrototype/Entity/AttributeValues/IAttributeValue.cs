using LandscapePrototype.Entity.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            switch (type)
            {
                case "text":
                    return AttributeValueText.Build(value);
                case "integer":
                    return AttributeValueInteger.Build(value);
                default:
                    throw new Exception($"Unknown type {type} encountered");
            }
        }

        public static string GetTypeString(IAttributeValue av)
        {
            switch (av)
            {
                case AttributeValueText text:
                    return "text";
                case AttributeValueInteger integer:
                    return "integer";
                default:
                    throw new Exception($"Unknown AttributeValue {av} encountered");
            }
        }

        public static (string, string) GetTypeAndValueString(IAttributeValue av)
        {
            return (GetTypeString(av), av.Value2String());
        }
    }
}
