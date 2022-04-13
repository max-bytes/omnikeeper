using System;
using System.IO;
using System.Text.Json;

namespace Omnikeeper.Base.Utils
{
    public class SystemTextJSONSerializer<T> where T : class
    {
        public SystemTextJSONSerializer(Func<JsonSerializerOptions> serializerOptionsF) : this(serializerOptionsF())
        {
        }
        public SystemTextJSONSerializer(JsonSerializerOptions serializerOptions)
        {
            SerializerOptions = serializerOptions;
        }
        private readonly JsonSerializerOptions SerializerOptions;

        public T Deserialize(string str)
        {
            var r = JsonSerializer.Deserialize<T>(str, SerializerOptions);
            if (r == null)
                throw new Exception("Could not deserialize string");
            return r;
        }
        public T Deserialize(ReadOnlySpan<char> span)
        {
            var r = JsonSerializer.Deserialize<T>(span, SerializerOptions);
            if (r == null)
                throw new Exception("Could not deserialize string");
            return r;
        }
        public T Deserialize(Stream stream)
        {
            var t = JsonSerializer.Deserialize<T>(stream);
            if (t == null)
                throw new Exception("Could not deserialize stream");
            return t;
        }

        public string SerializeToString(T t)
        {
            return JsonSerializer.Serialize(t, SerializerOptions);
        }
    }

    public static class SystemTextJSONSerializerMigrationHelper
    {
        public static string GetTypeString(Type type) => $"{type.FullName!}, {type.Assembly.GetName().Name}";
    }
}
