using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Omnikeeper.Base.Utils
{
    public class NewtonSoftJSONSerializer<T> where T : class
    {
        public NewtonSoftJSONSerializer(Func<JsonSerializerSettings> serializerSettingsF) : this(serializerSettingsF())
        {
        }
        public NewtonSoftJSONSerializer(JsonSerializerSettings serializerSettings)
        {
            SerializerSettings = serializerSettings;
            Serializer = JsonSerializer.Create(SerializerSettings);
        }
        private readonly JsonSerializerSettings SerializerSettings;
        private readonly JsonSerializer Serializer;

        public T Deserialize(JToken jo)
        {
            var r = Serializer.Deserialize<T>(new JTokenReader(jo));
            if (r == null)
                throw new Exception("Could not deserialize JToken");
            return r;
        }
        public object Deserialize(JToken jo, Type type)
        {
            var r = Serializer.Deserialize(new JTokenReader(jo), type);
            if (r == null)
                throw new Exception("Could not deserialize JToken");
            return r;
        }
        public T Deserialize(string str)
        {
            var r = JsonConvert.DeserializeObject<T>(str, SerializerSettings);
            if (r == null)
                throw new Exception("Could not deserialize string");
            return r;
        }
        public T Deserialize(ReadOnlySpan<char> span)
        {
            var str = span.ToString(); // TODO: newtonsoft JSON does not support working with spans directly, so we need to transform to a string, losing perf
            var r = JsonConvert.DeserializeObject<T>(str, SerializerSettings);
            if (r == null)
                throw new Exception("Could not deserialize span");
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
        public void SerializeToTextWriter(T config, TextWriter textWriter)
        {
            Serializer.Serialize(textWriter, config);
        }

        public T Deserialize(Stream stream)
        {
            using var tr = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(tr);
            var t = Serializer.Deserialize<T>(jsonReader);
            if (t == null)
                throw new Exception("Could not deserialize stream");
            return t;
        }
    }
}
