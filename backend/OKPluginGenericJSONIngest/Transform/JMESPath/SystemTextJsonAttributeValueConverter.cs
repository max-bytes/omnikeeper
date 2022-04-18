using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    class SystemTextJsonAttributeValueConverter : JsonConverter<IAttributeValue>
    {
        public override IAttributeValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            AttributeValueType? type = null;
            JsonElement valueObj = default;
            bool valueSet = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (type.HasValue && valueSet)
                    {
                        var value = AttributeValueHelper.BuildFromTypeAndJsonElement(type.Value, ref valueObj);
                        return value;
                    }
                    else if (type.HasValue && !valueSet)
                    {
                        return null;
                    } else
                    {
                        throw new JsonException();
                    }
                }

                // Get the key.
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "type":
                            type = JsonSerializer.Deserialize<AttributeValueType>(ref reader, options);
                            break;
                        case "value":
                            valueObj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                            valueSet = true;
                            break;
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, IAttributeValue value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("type");
            JsonSerializer.Serialize(writer, value.Type, options);
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value.ToGenericObject(), options);

            writer.WriteEndObject();
        }
    }
}
