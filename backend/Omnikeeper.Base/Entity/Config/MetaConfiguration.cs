using Omnikeeper.Base.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity.Config
{
    public class MetaConfiguration
    {
        private readonly string[] configLayers;
        private readonly string configWriteLayer;

        public string[] ConfigLayers => configLayers;
        public string ConfigWriteLayer => configWriteLayer;

        [JsonIgnore]
        public LayerSet ConfigLayerset => new LayerSet(configLayers);

        public static SystemTextJSONSerializer<MetaConfiguration> SystemTextJSONSerializer = new SystemTextJSONSerializer<MetaConfiguration>(new JsonSerializerOptions()
        {
        });

        public MetaConfiguration(string[] configLayers, string configWriteLayer)
        {
            this.configLayers = configLayers;
            this.configWriteLayer = configWriteLayer;
        }
    }
}
