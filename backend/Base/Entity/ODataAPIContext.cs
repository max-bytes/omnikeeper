
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Landscape.Base.Entity
{
    public class ODataAPIContext
    {
        public interface IConfig { }

        public class ConfigV3 : IConfig
        {
            public long WriteLayerID { get; set; }
            public long[] ReadLayerset { get; set; }
        }

        public string ID { get; set; }
        public IConfig CConfig { get; set; }

        public static IConfig DeserializeConfig(string s)
        {
            return JsonConvert.DeserializeObject<IConfig>(s, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });
        }

        // TODO: use one serializer for everything... but newtonsoft json API is confusing
        private static readonly JsonSerializer Serializer = new JsonSerializer()
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        public static IConfig DeserializeConfig(JObject jo)
        {
            return Serializer.Deserialize<IConfig>(new JTokenReader(jo));
        }

        public static JObject SerializeConfigToJObject(IConfig config)
        {
            return JObject.FromObject(config, Serializer);
        }
        public static string SerializeConfigToString(IConfig config)
        {
            return JsonConvert.SerializeObject(config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects });
        }
    }
}
