using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    [ProtoContract(SkipConstructor = true)]
    public class AttributeScalarValueImage : IAttributeScalarValue<BinaryScalarAttributeValueProxy>, IEquatable<AttributeScalarValueImage>
    {
        [ProtoMember(1)]
        private readonly BinaryScalarAttributeValueProxy value;
        public BinaryScalarAttributeValueProxy Value => value;
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues()
        {
            dynamic dynamicObject = new ExpandoObject();
            dynamicObject.hash = Value.Sha256Hash;
            dynamicObject.fullSize = Value.FullSize;
            return new string[] { JsonConvert.SerializeObject(dynamicObject) };
        }
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Image: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Image;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueImage);
        public bool Equals([AllowNull] AttributeScalarValueImage other) => other != null && Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public AttributeScalarValueImage(BinaryScalarAttributeValueProxy proxy)
        {
            this.value = proxy;
        }
    }

    [ProtoContract(SkipConstructor = true)]
    public class AttributeArrayValueImage : AttributeArrayValue<AttributeScalarValueImage, BinaryScalarAttributeValueProxy>
    {
        protected AttributeArrayValueImage(AttributeScalarValueImage[] values) : base(values)
        {
        }

        public override AttributeValueType Type => AttributeValueType.Image;

        public static AttributeArrayValueImage Build(IEnumerable<BinaryScalarAttributeValueProxy> proxies)
        {
            return new AttributeArrayValueImage(
                proxies.Select(p => new AttributeScalarValueImage(p)).ToArray()
            );
        }
    }
}
