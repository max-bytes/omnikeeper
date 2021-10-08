using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity.Config
{
    public class MetaConfiguration
    {
        private readonly string[] configLayerset;
        private readonly string configWriteLayer;

        [JsonProperty(Required = Required.Always)]
        public string[] ConfigLayerset => configLayerset;
        [JsonProperty(Required = Required.Always)]
        public string ConfigWriteLayer => configWriteLayer;

        public static MyJSONSerializer<MetaConfiguration> Serializer = new MyJSONSerializer<MetaConfiguration>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public MetaConfiguration(string[] configLayerset, string configWriteLayer)
        {
            this.configLayerset = configLayerset;
            this.configWriteLayer = configWriteLayer;
        }
    }
}
