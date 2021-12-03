using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginVariableRendering
{
    public class Configuration
    {
        [JsonProperty("input_layerset", Required = Required.Always)]
        public List<string> InputLayerSet { get; set; }

        [JsonProperty("target_layer", Required=Required.Always)]
        public string TargetLayer { get; set; }

        [JsonProperty("base_ci", Required = Required.Always)]
        public BaseCI BaseCI { get; set; }
        
        public Configuration()
        {
            TargetLayer = "";
            InputLayerSet = new List<string>();
            BaseCI = new BaseCI();
        }
    }

    public class BaseCI
    {
        [JsonProperty("required_trait", Required = Required.Always)]
        public string RequiredTrait { get; set; }

        [JsonProperty("input_whitelist")]
        public List<string> InputWhitelist { get; set; }

        [JsonProperty("input_blacklist")]
        public List<string> InputBlacklist { get; set; }

        [JsonProperty("attribute_mapping", Required = Required.Always)]
        public List<AttributeMapping> AttributeMapping { get; set; }

        [JsonProperty("follow_relations", Required = Required.Always)]
        public List<FollowRelation> FollowRelations { get; set; }

        public BaseCI()
        {
            RequiredTrait = "";
            InputWhitelist = new List<string>();
            InputBlacklist = new List<string>();
            AttributeMapping = new List<AttributeMapping>();
            FollowRelations = new List<FollowRelation>();
        }
    }

    public class FollowRelation
    {
        [JsonProperty("follow")]
        public List<Follow> Follow { get; set; }

        public FollowRelation()
        {
            Follow = new List<Follow>();
        }
    }

    public class Follow
    {
        [JsonProperty("predicate")]
        public string Predicate { get; set; }

        [JsonProperty("required_trait")]
        public string RequiredTrait { get; set; }

        [JsonProperty("input_blacklist")]
        public List<string> InputBlacklist { get; set; }

        [JsonProperty("input_whitelist")]
        public List<string> InputWhitelist { get; set; }

        [JsonProperty("attribute_mapping")]
        public List<AttributeMapping> AttributeMapping { get; set; }

        public Follow()
        {
            Predicate = "";
            RequiredTrait = "";
            InputBlacklist = new List<string>();
            InputWhitelist = new List<string>();
            AttributeMapping = new List<AttributeMapping>();
        }
    }

    public class AttributeMapping
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        public AttributeMapping()
        {
            Source = "*"; // NOTE this mean that all attributes should be taken into account.
            Target = "";
        }
    }
}
