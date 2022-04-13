using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Utils
{
    public class TypeDiscriminatorConverter<T> : JsonConverter<T> where T : class
    {
        private readonly IDictionary<string, Type> validTypeNames;
        private readonly string discriminatorPropertyName; // TODO: needed?
        private readonly string jsonPropertyName;

        public TypeDiscriminatorConverter(string discriminatorPropertyName, string? jsonPropertyName)
        {
            var type = typeof(T);
            validTypeNames = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToDictionary(t => SystemTextJSONSerializerMigrationHelper.GetTypeString(t), t => t);
            this.discriminatorPropertyName = discriminatorPropertyName;
            this.jsonPropertyName = jsonPropertyName ?? discriminatorPropertyName;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using (var jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                if (!jsonDocument.RootElement.TryGetProperty(jsonPropertyName, out var typeProperty))
                {
                    throw new JsonException();
                }

                var typeName = typeProperty.GetString();
                if (typeName == null)
                {
                    throw new JsonException();
                }
                if (!validTypeNames.TryGetValue(typeName, out var type))
                {
                    throw new JsonException();
                }

                var result = (T?)JsonSerializer.Deserialize(jsonDocument, type, options);

                if (result == null)
                    throw new JsonException();

                return result;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
