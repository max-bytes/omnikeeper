//using LandscapeRegistry.Entity.AttributeValues;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Threading.Tasks;

//namespace LandscapeRegistry.Entity.Converters
//{
//    public class AttributeValueConverter : JsonConverter<IAttributeValue>
//    {
//        public override bool CanConvert(Type typeToConvert) =>
//            typeof(IAttributeValue).IsAssignableFrom(typeToConvert);

//        public override IAttributeValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//        {
//            if (reader.TokenType != JsonTokenType.StartObject)
//            {
//                throw new JsonException();
//            }

//            reader.Read();
//            if (reader.TokenType != JsonTokenType.PropertyName)
//            {
//                throw new JsonException();
//            }

//            string propertyName = reader.GetString();
//            if (propertyName != "type")
//            {
//                throw new JsonException();
//            }

//            reader.Read();
//            if (reader.TokenType != JsonTokenType.String)
//            {
//                throw new JsonException();
//            }

//            string typeStr = reader.GetString();
//            if (!Enum.TryParse<AttributeValueType>(typeStr, out var type))
//                throw new JsonException();

//            reader.Read();
//            if (reader.TokenType != JsonTokenType.PropertyName)
//            {
//                throw new JsonException();
//            }

//            string valueName = reader.GetString();
//            if (valueName != "value")
//            {
//                throw new JsonException();
//            }

//            reader.Read();
//            if (reader.TokenType != JsonTokenType.String)
//            {
//                throw new JsonException();
//            }

//            string value = reader.GetString();

//            try
//            {
//                return AttributeValueBuilder.Build(type, value);
//            } catch (Exception e)
//            {
//                throw new JsonException("Failed to build AttributeValue", e);
//            }
//        }

//        public override void Write(Utf8JsonWriter writer, IAttributeValue av, JsonSerializerOptions options)
//        {
//            var (type, strValue) = AttributeValueBuilder.GetTypeAndValueString(av);
//            writer.WriteStartObject();
//            writer.WriteString("type", type.ToString());
//            writer.WriteString("value", strValue);
//            writer.WriteEndObject();
//        }
//    }
//}
