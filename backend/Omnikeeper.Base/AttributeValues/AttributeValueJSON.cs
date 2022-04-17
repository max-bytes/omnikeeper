using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Omnikeeper.Entity.AttributeValues
{
    public class AttributeScalarValueJSONNew : IAttributeScalarValue<JsonDocument>, IEquatable<AttributeScalarValueJSONNew>
    {
        private readonly JsonDocument value;
        private readonly string valueStr;
        public override string ToString() => $"AV-JSON: {Value2String()}";
        public JsonDocument Value => value;
        public AttributeValueType Type => AttributeValueType.JSON;
        public bool IsArray => false;
        public bool Equals([AllowNull] IAttributeValue? other) => Equals(other as AttributeScalarValueJSONNew);
        public bool Equals([AllowNull] AttributeScalarValueJSONNew other)
        {
            return other != null && Value.RootElement.GetRawText() == other.Value.RootElement.GetRawText(); // TODO: implement proper deep equality
        }
        public override int GetHashCode() => Value.GetHashCode();
        public object ToGenericObject() => Value;
        public object ToGraphQLValue() => valueStr;
        public string[] ToRawDTOValues() => new string[] { valueStr };
        public string Value2String() => valueStr;
        public static IAttributeValue BuildFromString(string v)
        {
            try
            {
                var vv = JsonDocument.Parse(v);
                if (vv == null)
                    throw new Exception("Could not parse JsonDocument from string");
                return Build(vv);
            }
            catch (Exception e)
            {
                throw new Exception("Error building JSON attribute value from string", e);
            }
        }

        public static IAttributeValue Build(JsonDocument t)
        {
            if (t.RootElement.ValueKind == JsonValueKind.Array)
            {
                var documents = t.RootElement.EnumerateArray().Select(e => JsonDocument.Parse(e.GetRawText()));
                return AttributeArrayValueJSONNew.Build(documents);
            }
            else
            {
                return new AttributeScalarValueJSONNew(t, t.RootElement.GetRawText());
            }
        }

        private AttributeScalarValueJSONNew(JsonDocument v, string valueStr)
        {
            this.value = v;
            this.valueStr = valueStr;
        }
    }

    public class AttributeArrayValueJSONNew : AttributeArrayValue<AttributeScalarValueJSONNew, JsonDocument>
    {
        protected AttributeArrayValueJSONNew(AttributeScalarValueJSONNew[] values) : base(values)
        {
        }

#pragma warning disable CS8618
        protected AttributeArrayValueJSONNew() { }
#pragma warning restore CS8618

        public override AttributeValueType Type => AttributeValueType.JSON;

        public static AttributeArrayValueJSONNew BuildFromString(string[] values)
        {
            var jsonValues = values.Select(value =>
            {
                try
                {
                    return JsonDocument.Parse(value);
                }
                catch (Exception e)
                {
                    throw new Exception("Error building JSON attribute value from string", e);
                }
            }).ToArray();
            return Build(jsonValues);
        }

        public static AttributeArrayValueJSONNew Build(IEnumerable<JsonDocument> values)
        {
            var n = new AttributeArrayValueJSONNew(
                values.Select(v => {
                    var element = AttributeScalarValueJSONNew.Build(v);
                    if (element is not AttributeScalarValueJSONNew jsonElement)
                        throw new Exception("Expected every element of AttributeArrayValueJSON to be object, not array");
                    return jsonElement;
                }).ToArray()
            );
            return n;
        }
    }


//    public class AttributeScalarValueJSON : IAttributeScalarValue<JToken>, IEquatable<AttributeScalarValueJSON>
//    {
//        public override string ToString() => $"AV-JSON: {Value2String()}";

//        private readonly JToken value;
//        public JToken Value => value;
//        private readonly string valueStr;
//        public string ValueStr => valueStr;

//        public string Value2String() => valueStr;
//        public string[] ToRawDTOValues() => new string[] { valueStr };
//        public object ToGenericObject() => Value;
//        public bool IsArray => false;
//        public object ToGraphQLValue() => valueStr;

//        public AttributeValueType Type => AttributeValueType.JSON;

//        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeScalarValueJSON);
//        public bool Equals([AllowNull] AttributeScalarValueJSON other) => other != null && JToken.DeepEquals(Value, other.Value);
//        public override int GetHashCode() => Value.GetHashCode();

//        public static AttributeScalarValueJSON BuildFromString(string value)
//        {
//            try
//            {
//                var v = JToken.Parse(value);
//                return new AttributeScalarValueJSON(v, v.ToString());
//            }
//            catch (JsonReaderException e)
//            {
//                throw new Exception("Error building JSON attribute value from string", e);
//            }
//        }

//        public static AttributeScalarValueJSON Build(JToken value)
//        {
//            return new AttributeScalarValueJSON(value, value.ToString());
//        }

//        private AttributeScalarValueJSON(JToken value, string valueStr)
//        {
//            this.value = value;
//            this.valueStr = valueStr;
//        }
//    }

//    public class AttributeArrayValueJSON : AttributeArrayValue<AttributeScalarValueJSON, JToken>
//    {
//        protected AttributeArrayValueJSON(AttributeScalarValueJSON[] values) : base(values)
//        {
//        }

//#pragma warning disable CS8618
//        protected AttributeArrayValueJSON() { }
//#pragma warning restore CS8618

//        public override AttributeValueType Type => AttributeValueType.JSON;

//        public static AttributeArrayValueJSON BuildFromString(string[] values)
//        {
//            var jsonValues = values.Select(value =>
//            {
//                try
//                {
//                    return JToken.Parse(value);
//                }
//                catch (JsonReaderException e)
//                {
//                    throw new Exception("Error building JSON attribute value from string", e);
//                }
//            }).ToArray();
//            return Build(jsonValues);
//        }

//        public static AttributeArrayValueJSON Build(IEnumerable<JToken> values)
//        {
//            var n = new AttributeArrayValueJSON(
//                values.Select(v => AttributeScalarValueJSON.Build(v)).ToArray()
//            );
//            return n;
//        }
//    }
}
