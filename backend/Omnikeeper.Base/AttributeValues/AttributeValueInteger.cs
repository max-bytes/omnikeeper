using ProtoBuf;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    [ProtoContract(SkipConstructor = true)]
    public class AttributeScalarValueInteger : IAttributeScalarValue<long>, IEquatable<AttributeScalarValueInteger>
    {
        [ProtoMember(1)] private readonly long value;
        public long Value => value;
        public string Value2String() => Value.ToString();
        public string[] ToRawDTOValues() => new string[] { Value.ToString() };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public override string ToString() => $"AV-Integer: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Integer;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueInteger);
        public bool Equals([AllowNull] AttributeScalarValueInteger other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        public AttributeScalarValueInteger(long value)
        {
            this.value = value;
        }

        public static AttributeScalarValueInteger BuildFromString(string value)
        {
            long.TryParse(value, out var v);
            return new AttributeScalarValueInteger(v);
        }

    }

    [ProtoContract]
    public class AttributeArrayValueInteger : AttributeArrayValue<AttributeScalarValueInteger, long>
    {
        public AttributeArrayValueInteger(AttributeScalarValueInteger[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueInteger() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => AttributeValueType.Integer;

        public static AttributeArrayValueInteger Build(long[] values)
        {
            return new AttributeArrayValueInteger(
                values.Select(v => new AttributeScalarValueInteger(v)).ToArray()
            );
        }

        public static AttributeArrayValueInteger BuildFromString(string[] values)
        {
            var longValues = values.Select(value => { long.TryParse(value, out var v); return v; }).ToArray();
            return Build(longValues);
        }
    }
}
