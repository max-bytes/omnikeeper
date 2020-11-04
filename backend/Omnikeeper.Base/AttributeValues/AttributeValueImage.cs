using Newtonsoft.Json;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueImage : IAttributeScalarValue<BinaryScalarAttributeValueProxy>, IEquatable<AttributeScalarValueImage>
    {
        public BinaryScalarAttributeValueProxy Value { get; private set; }
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

        public static AttributeScalarValueImage Build(BinaryScalarAttributeValueProxy proxy)
        {
            return new AttributeScalarValueImage
            {
                Value = proxy
            };
        }
    }


    public class AttributeArrayValueImage : AttributeArrayValue<AttributeScalarValueImage, BinaryScalarAttributeValueProxy>
    {
        public override AttributeValueType Type => AttributeValueType.Image;

        public static AttributeArrayValueImage Build(IEnumerable<BinaryScalarAttributeValueProxy> proxies)
        {
            return new AttributeArrayValueImage()
            {
                Values = proxies.Select(p => AttributeScalarValueImage.Build(p)).ToArray()
            };
        }
    }
}
