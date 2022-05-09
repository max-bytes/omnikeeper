using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OKPluginCLBNaemonVariableResolution
{
    public class Configuration
    {
        [JsonPropertyName("cmdb_input_layerset")]
        public List<string> CMDBInputLayerSet { get; set; }

        [JsonPropertyName("monman_v1_input_layerset")]
        public List<string> MonmanV1InputLayerSet { get; set; }

        public Configuration()
        {
            CMDBInputLayerSet = new List<string>();
            MonmanV1InputLayerSet = new List<string>();
        }
    }
}
