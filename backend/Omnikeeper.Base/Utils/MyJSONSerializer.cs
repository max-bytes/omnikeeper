using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Omnikeeper.Base.Utils
{
    public class MyJSONSerializer<T> where T : class
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
            var r = Serializer.Deserialize<T>(new JTokenReader(jo));
            if (r == null)
                throw new Exception("Could not deserialize JObject");
            return r;
        }
        public T Deserialize(string str)
        {
            var r = JsonConvert.DeserializeObject<T>(str, SerializerSettings);
            if (r == null)
                throw new Exception("Could not deserialize string");
            return r;
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
