using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueDouble : IAttributeScalarValue<double>, IEquatable<AttributeScalarValueDouble>
    {
        private readonly double value;
        public double Value => value;
        public string Value2String() => Value.ToString(CultureInfo.InvariantCulture);
        public string[] ToRawDTOValues() => new string[] { Value.ToString(CultureInfo.InvariantCulture) };
        public object ToGenericObject() => Value;
        public bool IsArray => false;
        public object ToGraphQLValue() => Value;

        public byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override string ToString() => $"AV-Double: {Value2String()}";

        public AttributeValueType Type => AttributeValueType.Double;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueDouble);
        public bool Equals([AllowNull] AttributeScalarValueDouble other) => other != null && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        public AttributeScalarValueDouble(double value)
        {
            this.value = value;
        }

        public static AttributeScalarValueDouble BuildFromBytes(byte[] value)
        {
            var v = BitConverter.ToDouble(value, 0);
            return new AttributeScalarValueDouble(v);
        }

        public static AttributeScalarValueDouble BuildFromString(string value)
        {
            if (double.TryParse(value, NumberStyle, CultureInfo.InvariantCulture, out var v))
                return new AttributeScalarValueDouble(v);
            else
                throw new Exception("Could not parse double for attribute value");
        }

        public const NumberStyles NumberStyle = NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowTrailingWhite |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent;
    }

    public class AttributeArrayValueDouble : AttributeArrayValue<AttributeScalarValueDouble, double>
    {
        public AttributeArrayValueDouble(AttributeScalarValueDouble[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueDouble() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => AttributeValueType.Double;

        public static AttributeArrayValueDouble Build(double[] values)
        {
            return new AttributeArrayValueDouble(
                values.Select(v => new AttributeScalarValueDouble(v)).ToArray()
            );
        }

        // NOTE: assumes doubles are packed one after the other
        internal static IAttributeValue BuildFromBytes(byte[] bytes)
        {
            var num = bytes.Length / 8;
            var arr = new double[num];
            for(var i = 0;i < num;i++)
            {
                var v = BitConverter.ToDouble(bytes, i * 8);
                arr[i] = v;
            }
            return Build(arr);
        }
        public static AttributeArrayValueDouble BuildFromString(string[] values)
        {
            var doubleValues = values.Select(value => {
                if (double.TryParse(value, AttributeScalarValueDouble.NumberStyle, CultureInfo.InvariantCulture, out var v))
                    return v;
                else
                    throw new Exception("Could not parse double for attribute value");
            }).ToArray();
            return Build(doubleValues);
        }
    }
}
