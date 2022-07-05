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

    public sealed record class AttributeArrayValueImage(AttributeScalarValueImage[] Values) : AttributeArrayValue<AttributeScalarValueImage, BinaryScalarAttributeValueProxy>(Values)
    {
        public override AttributeValueType Type => AttributeValueType.Image;

        public static AttributeArrayValueImage Build(IEnumerable<BinaryScalarAttributeValueProxy> proxies)
        {
            return new AttributeArrayValueImage(
                proxies.Select(p => new AttributeScalarValueImage(p)).ToArray()
            );
        }
    }
}
