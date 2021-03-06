using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Omnikeeper.Entity.AttributeValues
{
    // NOTE: not a record class because we do things like lazy initialization
    public sealed class AttributeScalarValueJSON : IAttributeScalarValue<JsonDocument>, IEquatable<AttributeScalarValueJSON>
    {
        private JsonDocument? value;
        private readonly string valueStr;

        // internal getter method, used to lazily initialize the JsonDocument
        private JsonDocument GetValue()
        {
            if (value == null)
            {
                var v = JsonDocument.Parse(valueStr);
                value = v;
                return v;
            }
            else { return value; }
        }

        public string ValueStr => valueStr;
        public override string ToString() => $"AV-JSON: {Value2String()}";
        public JsonDocument Value => GetValue();
        public AttributeValueType Type => AttributeValueType.JSON;
        public bool IsArray => false;
        public bool Equals([AllowNull] IAttributeValue? other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals([AllowNull] AttributeScalarValueJSON other)
        {
            // NOTE: we do basic string equality comparison, because other methods would be much more expensive and ideal either
            // this means that tiny changes to the JSON, like reordering of member order or whitespace means the attribute value has changed
            // make sure to consider that and take care that the produced JSONs are as stable as possible
            return other != null && valueStr == other.valueStr;
        }
        public override int GetHashCode() => valueStr.GetHashCode();
        public object ToGenericObject() => GetValue();
        public object ToGraphQLValue() => valueStr;
        public string[] ToRawDTOValues() => new string[] { valueStr };
        public string Value2String() => valueStr;
        public static IAttributeValue BuildFromString(string v, bool parse)
        {
            try
            {
                if (parse)
                {
                    var vv = JsonDocument.Parse(v);
                    if (vv == null)
                        throw new Exception("Could not parse JsonDocument from string");
                    return BuildFromJsonDocument(vv);
                }
                else
                {
                    return new AttributeScalarValueJSON(v);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error building JSON attribute value from string", e);
            }
        }

        public static IAttributeValue BuildFromJsonDocument(JsonDocument t)
        {
            if (t.RootElement.ValueKind == JsonValueKind.Array)
            {
                var elements = t.RootElement.EnumerateArray().Select(e => e.ToString()); // TODO
                return AttributeArrayValueJSON.BuildFromString(elements, false);
            }
            else
            {
                return new AttributeScalarValueJSON(t);
            }
        }

        // TODO: the whole method seems very hacky and slow
        public static IAttributeValue BuildFromJsonElement(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Array)
            {
                var elements = el.EnumerateArray().Select(e =>
                {
                    return e.ToString();
                }); // TODO
                return AttributeArrayValueJSON.BuildFromString(elements, false);
            }
            else
            {
                return new AttributeScalarValueJSON(el.ToString()); // TODO
            }
        }

        private AttributeScalarValueJSON(string valueStr)
        {
            this.value = null;
            this.valueStr = valueStr;
        }

        private AttributeScalarValueJSON(JsonDocument v)
        {
            this.value = v;
            this.valueStr = v.RootElement.ToString();
        }
    }

    public sealed record class AttributeArrayValueJSON(AttributeScalarValueJSON[] Values) : AttributeArrayValue<AttributeScalarValueJSON, JsonDocument>(Values)
    {
        public override AttributeValueType Type => AttributeValueType.JSON;

        public static AttributeArrayValueJSON BuildFromString(IEnumerable<string> values, bool parse)
        {
            var elements = values.Select(value =>
            {
                var element = AttributeScalarValueJSON.BuildFromString(value, parse);
                if (element is not AttributeScalarValueJSON jsonElement)
                    throw new Exception("Expected every element of AttributeArrayValueJSON to be object, not array");
                return jsonElement;
            }).ToArray();
            return new AttributeArrayValueJSON(elements);
        }

        public static AttributeArrayValueJSON BuildFromJsonDocuments(IEnumerable<JsonDocument> values)
        {
            var n = new AttributeArrayValueJSON(
                values.Select(v =>
                {
                    var element = AttributeScalarValueJSON.BuildFromJsonDocument(v);
                    if (element is not AttributeScalarValueJSON jsonElement)
                        throw new Exception("Expected every element of AttributeArrayValueJSON to be object, not array");
                    return jsonElement;
                }).ToArray()
            );
            return n;
        }
    }
}
