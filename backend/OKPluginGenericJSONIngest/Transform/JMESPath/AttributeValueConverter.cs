using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    // NOTE: this converter exists so that the attribute value is - when it comes in as an array - properly deserialized into an array of X
    // (where X is the type of the item of the array)
    // without this custom converter, an array is deserialized into a generic JObject, which is not what we want
    class AttributeValueConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var array = new List<object>();
                reader.Read();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    var item = serializer.Deserialize(reader, objectType);
                    array.Add(item);
                    reader.Read();
                }
                return array.ToArray();
            }
            else
                return serializer.Deserialize(reader, objectType);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
