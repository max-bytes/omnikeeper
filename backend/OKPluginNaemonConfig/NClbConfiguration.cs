using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OKPluginNaemonConfig
{
    public class NClbConfiguration
    {
        [JsonPropertyName("monman_layer_id")]
        public string MonmanLayerId { get; set; }

        [JsonPropertyName("cmdb_layer_id")]
        public string CMDBLayerId { get; set; }

        [JsonPropertyName("static_template_command")]
        public StaticTemplateCommand Command { get; set; }

        [JsonPropertyName("load-cmdb-customer")]
        public List<string> LoadCMDBCustomer { get; set; }

        [JsonPropertyName("cmdb-monprofile-prefix")]
        public List<string> CMDBMonprofilePrefix { get; set; }

        [JsonPropertyName("naemons-config-generateprofiles")]
        public List<string> NaemonsConfigGenerateprofiles { get; set; }

        public NClbConfiguration()
        {
            MonmanLayerId = "";
            CMDBLayerId = "";
            LoadCMDBCustomer = new List<string>();
            CMDBMonprofilePrefix = new List<string>();
            NaemonsConfigGenerateprofiles = new List<string>();
            Command = new();
        }
    }

    public class StaticTemplateCommand
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("line")]
        public string Line { get; set; }

        public StaticTemplateCommand()
        {
            Name = "";
            Line = "";
        }
    }
}
