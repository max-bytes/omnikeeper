using System;

namespace Omnikeeper.Entity.AttributeValues
{
    public interface IAttributeValueMask
    {
    }

    public sealed record class AttributeScalarValueMask() : IAttributeScalarValue<object>, IAttributeValueMask
    {
        private static readonly object o = new object();
        private static readonly string[] oArray = new string[] { "" };

        public string Value2String() => "";
        public string[] ToRawDTOValues() => oArray;
        public object ToGenericObject() => o;
        public bool IsArray => false;
        public object ToGraphQLValue() => o;

        public override string ToString() => $"AV-Mask";

        public AttributeValueType Type => AttributeValueType.Mask;

        public object Value => o;

        public bool Equals(IAttributeValue? other) => Equals(other as AttributeScalarValueMask);

        public static readonly AttributeScalarValueMask Instance = new AttributeScalarValueMask();
    }
}
