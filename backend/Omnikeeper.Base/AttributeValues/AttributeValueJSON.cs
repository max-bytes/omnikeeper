using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using ProtoBuf.Serializers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Entity.AttributeValues
{
    [ProtoContract(Serializer = typeof(AttributeScalarValueJSONSerializer))]
    public class AttributeScalarValueJSON : IAttributeScalarValue<JToken>, IEquatable<AttributeScalarValueJSON>
    {
        public static JToken ErrorValue(string message) => JToken.Parse($"{{\"error\": \"{message}\" }}");

        public override string ToString() => $"AV-JSON: {Value2String()}";

        private readonly JToken value;
        public JToken Value => value;
        private readonly string valueStr;
        public string ValueStr => valueStr;

        public string Value2String() => valueStr;
        public string[] ToRawDTOValues() => new string[] { valueStr };
        public object ToGenericObject() => Value;
        public bool IsArray => false;

        public AttributeValueType Type => AttributeValueType.JSON;

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals([AllowNull] AttributeScalarValueJSON other) => other != null && JToken.DeepEquals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public static AttributeScalarValueJSON BuildFromString(string value)
        {
            try
            {
                var v = JToken.Parse(value);
                return new AttributeScalarValueJSON(v, v.ToString());
            }
            catch (JsonReaderException e)
            {
                var eJson = ErrorValue(e.Message); // TODO: we need to handling this different, probabl throw the error and have other systems deal with it
                return new AttributeScalarValueJSON(eJson, eJson.ToString());
            }
        }

        public static AttributeScalarValueJSON Build(JToken value)
        {
            return new AttributeScalarValueJSON(value, value.ToString());
        }

        private AttributeScalarValueJSON(JToken value, string valueStr)
        {
            this.value = value;
            this.valueStr = valueStr;
        }
    }

    public class AttributeScalarValueJSONSerializer : ISubTypeSerializer<AttributeScalarValueJSON>, ISerializer<AttributeScalarValueJSON>
    {
        SerializerFeatures ISerializer<AttributeScalarValueJSON>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;
        void ISerializer<AttributeScalarValueJSON>.Write(ref ProtoWriter.State state, AttributeScalarValueJSON value)
            => ((ISubTypeSerializer<AttributeScalarValueJSON>)this).WriteSubType(ref state, value);
        AttributeScalarValueJSON ISerializer<AttributeScalarValueJSON>.Read(ref ProtoReader.State state, AttributeScalarValueJSON value)
            => ((ISubTypeSerializer<AttributeScalarValueJSON>)this).ReadSubType(ref state, SubTypeState<AttributeScalarValueJSON>.Create(state.Context, value));

        public void WriteSubType(ref ProtoWriter.State state, AttributeScalarValueJSON value)
        {
            state.WriteFieldHeader(1, WireType.String);
            state.WriteString(value.ValueStr);
        }

        public AttributeScalarValueJSON ReadSubType(ref ProtoReader.State state, SubTypeState<AttributeScalarValueJSON> value)
        {
            int field;
            string valueStr = "";
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch (field)
                {
                    case 1:
                        valueStr = state.ReadString();
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            if (valueStr != "")
                return AttributeScalarValueJSON.BuildFromString(valueStr);
            else
                throw new Exception("Could not deserialize AttributeScalarValueJSON");
        }
    }

    [ProtoContract]
    public class AttributeArrayValueJSON : AttributeArrayValue<AttributeScalarValueJSON, JToken>
    {
        protected AttributeArrayValueJSON(AttributeScalarValueJSON[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueJSON() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => AttributeValueType.JSON;

        public static AttributeArrayValueJSON BuildFromString(string[] values)
        {
            var jsonValues = values.Select(value =>
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch (JsonReaderException e)
                { // TODO: we need to handling this different, probabl throw the error and have other systems deal with it
                    return AttributeScalarValueJSON.ErrorValue(e.Message);
                }
            }).ToArray();
            return Build(jsonValues);
        }

        public static AttributeArrayValueJSON Build(JToken[] values)
        {
            var n = new AttributeArrayValueJSON(
                values.Select(v => AttributeScalarValueJSON.Build(v)).ToArray()
            );
            return n;
        }
    }
}
