using Newtonsoft.Json;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Base.Entity.Config
{
    public class MetaConfiguration
    {
        private readonly string[] configLayers;
        private readonly string configWriteLayer;

        [JsonProperty(Required = Required.Always)]
        public string[] ConfigLayers => configLayers;
        [JsonProperty(Required = Required.Always)]
        public string ConfigWriteLayer => configWriteLayer;

        [JsonIgnore]
        public LayerSet ConfigLayerset => new LayerSet(configLayers);

        public static MyJSONSerializer<MetaConfiguration> Serializer = new MyJSONSerializer<MetaConfiguration>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Error
        });

        public MetaConfiguration(string[] configLayers, string configWriteLayer)
        {
            this.configLayers = configLayers;
            this.configWriteLayer = configWriteLayer;
        }
    }
}
