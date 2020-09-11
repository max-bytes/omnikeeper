using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Landscape.Base.Utils
{
    public class MyJSONSerializer<T>
    {
        public MyJSONSerializer(Func<JsonSerializerSettings> serializerSettingsF) : this(serializerSettingsF())
        {
        }
        public MyJSONSerializer(JsonSerializerSettings serializerSettings)
        {
            SerializerSettings = serializerSettings;
            Serializer = JsonSerializer.Create(SerializerSettings);
        }
        private readonly JsonSerializerSettings SerializerSettings;
        private readonly JsonSerializer Serializer;

        public T Deserialize(JObject jo)
        {
            return Serializer.Deserialize<T>(new JTokenReader(jo));
        }
        public T Deserialize(string str)
        {
            return JsonConvert.DeserializeObject<T>(str, SerializerSettings);
        }

        public JObject SerializeToJObject(T config)
        {
            return JObject.FromObject(config, Serializer);
        }
        public string SerializeToString(T config)
        {
            return JsonConvert.SerializeObject(config, SerializerSettings);
        }
    }
}
