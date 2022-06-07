using Omnikeeper.Base.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity.Config
{
    public class MetaConfiguration
    {
        private readonly string[] configLayers;
        private readonly string configWriteLayer;
        private readonly string[] issueLayers;
        private readonly string issueWriteLayer;

        public string[] ConfigLayers => configLayers;
        public string ConfigWriteLayer => configWriteLayer;

        public string[] IssueLayers => issueLayers;
        public string IssueWriteLayer => issueWriteLayer;

        [JsonIgnore]
        public LayerSet ConfigLayerset => new LayerSet(configLayers);

        [JsonIgnore]
        public LayerSet IssueLayerset => new LayerSet(issueLayers);

        public static SystemTextJSONSerializer<MetaConfiguration> SystemTextJSONSerializer = new SystemTextJSONSerializer<MetaConfiguration>(new JsonSerializerOptions()
        {
        });

        public MetaConfiguration(string[] configLayers, string configWriteLayer, string[] issueLayers, string issueWriteLayer)
        {
            this.configLayers = configLayers;
            this.configWriteLayer = configWriteLayer;
            this.issueLayers = issueLayers;
            this.issueWriteLayer = issueWriteLayer;
        }
    }
}
