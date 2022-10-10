using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Text.Json;

namespace Omnikeeper.Entity.AttributeValues
{
    public sealed record class AttributeScalarValueImage(BinaryScalarAttributeValueProxy Value) : IAttributeScalarValue<BinaryScalarAttributeValueProxy>
    {
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues()
        {
            dynamic dynamicObject = new ExpandoObject();
            dynamicObject.hash = Value.Sha256Hash;
            dynamicObject.fullSize = Value.FullSize;
            return new string[] { JsonSerializer.Serialize(dynamicObject) };
        }
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public override string ToString() => $"AV-Image: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Image;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueImage);
    }

    public sealed record class AttributeArrayValueImage(AttributeScalarValueImage[] Values) : IAttributeArrayValue
    {
        public AttributeValueType Type => AttributeValueType.Image;

        public int Length => Values.Length;
        public bool IsArray => true;

        public static AttributeArrayValueImage Build(IEnumerable<BinaryScalarAttributeValueProxy> proxies)
        {
            return new AttributeArrayValueImage(
                proxies.Select(p => new AttributeScalarValueImage(p)).ToArray()
            );
        }

        public bool Equals(AttributeArrayValueImage? other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        // TODO: simplify
        public string[] ToRawDTOValues() => Values.Select(v => v.ToRawDTOValues()[0]).ToArray();
        public object ToGenericObject() => Values.Select(v => v.Value).ToArray();
        public object ToGraphQLValue() => Values.Select(v => v.ToGraphQLValue()).ToArray();
        public string Value2String() => string.Join(",", Values.Select(value => value.Value2String().Replace(",", "\\,")));
    }
}
