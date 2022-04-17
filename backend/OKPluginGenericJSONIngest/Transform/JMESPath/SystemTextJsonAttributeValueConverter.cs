using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
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
            object? valueObj = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (type.HasValue && valueObj != null)
                    {
                        var value = AttributeValueHelper.BuildFromTypeAndObject(type.Value, valueObj);
                        return value;
                    }
                    else if (type.HasValue && valueObj == null)
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
                            valueObj = JsonSerializer.Deserialize<object>(ref reader, options);
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
