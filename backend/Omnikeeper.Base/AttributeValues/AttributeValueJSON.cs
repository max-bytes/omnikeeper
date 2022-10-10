using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Omnikeeper.Entity.AttributeValues
{
    // NOTE: not a record class because we do things like lazy initialization
    public sealed class AttributeScalarValueJSON : IAttributeScalarValue<JsonDocument>, IEquatable<AttributeScalarValueJSON>//, IEquatable<IAttributeValue>
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
        public override bool Equals(object? other) => Equals(other as AttributeScalarValueJSON);
        //public bool Equals(IAttributeValue? other) => Equals(other as AttributeScalarValueJSON);
        public bool Equals(AttributeScalarValueJSON? other)
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
                var elements = t.RootElement.EnumerateArray().Select(e => e.ToString()).ToArray(); // TODO
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
                return AttributeArrayValueJSON.BuildFromString(elements.ToArray(), false);
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

    // NOTE: not a record class because we do things like lazy initialization
    public sealed class AttributeArrayValueJSON : IAttributeArrayValue, IEquatable<AttributeArrayValueJSON>
    {
        private JsonDocument[]? values;
        private readonly string[] valuesStr;

        public string[] ValuesStr => valuesStr;

        // internal getter method, used to lazily initialize the JsonDocument
        private JsonDocument[] GetValues()
        {
            if (values == null)
                values = Parse(valuesStr).ToArray();
            return values;
        }

        private static IEnumerable<JsonDocument> Parse(string[] valuesStr)
        {
            return valuesStr.Select(value =>
            {
                var d = JsonDocument.Parse(value);
                if (d.RootElement.ValueKind == JsonValueKind.Array)
                    throw new Exception("Expected every element of AttributeArrayValueJSON to NOT be an array");
                return d;
            });
        }

        private AttributeArrayValueJSON(string[] valuesStr)
        {
            this.values = null;
            this.valuesStr = valuesStr;
        }

        public AttributeValueType Type => AttributeValueType.JSON;

        public int Length => valuesStr.Length;
        public bool IsArray => true;

        public static AttributeArrayValueJSON BuildFromString(string[] values, bool parse)
        {
            if (parse)
            {
                var documents = Parse(values).ToArray();
                return new AttributeArrayValueJSON(values)
                {
                    values = documents
                };
            }
            else
            {
                return new AttributeArrayValueJSON(values);
            }
        }

        public static AttributeArrayValueJSON BuildFromJsonDocuments(JsonDocument[] values)
        {
            var strings = values.Select(d =>
            {
                if (d.RootElement.ValueKind == JsonValueKind.Array)
                    throw new Exception("Expected every element of AttributeArrayValueJSON to NOT be an array");
                return d.RootElement.ToString();
            }).ToArray();
            return new AttributeArrayValueJSON(strings)
            {
                values = values
            };
        }

        public override bool Equals(object? other) => Equals(other as AttributeArrayValueJSON);
        public bool Equals(AttributeArrayValueJSON? other) => other != null && valuesStr.SequenceEqual(other.valuesStr);
        public override int GetHashCode() => valuesStr.GetHashCode();

        public string[] ToRawDTOValues() => valuesStr;
        public object ToGenericObject() => GetValues();
        public object ToGraphQLValue() => valuesStr;
        public string Value2String() => string.Join(",", valuesStr.Select(value => value.Replace(",", "\\,")));
    }
}
