using System.Text.Json.Serialization;

namespace OKPluginCLBNaemonVariableResolution
{
    public enum Stage
    {
        Dev, Prod
    }

    public class Configuration
    {
        [JsonPropertyName("cmdb_input_layerset")]
        public List<string> CMDBInputLayerSet { get; set; }

        [JsonPropertyName("monman_v1_input_layerset")]
        public List<string> MonmanV1InputLayerSet { get; set; }

        [JsonPropertyName("selfservice_variables_input_layerset")]
        public List<string> SelfserviceVariablesInputLayerSet { get; set; }

        [JsonPropertyName("debug_target_cmdb_id")]
        public string? DebugTargetCMDBID { get; set; }

        [JsonPropertyName("stage")]
        public Stage Stage { get; set; }
        public Configuration()
        {
            CMDBInputLayerSet = new List<string>();
            MonmanV1InputLayerSet = new List<string>();
            SelfserviceVariablesInputLayerSet = new List<string>();
            DebugTargetCMDBID = null;
        }
    }
}
