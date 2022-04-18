using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OKPluginVariableRendering
{
    public class Configuration
    {
        [JsonPropertyName("input_layerset")]
        public List<string> InputLayerSet { get; set; }

        [JsonPropertyName("base_ci")]
        public BaseCI BaseCI { get; set; }
        
        public Configuration()
        {
            InputLayerSet = new List<string>();
            BaseCI = new BaseCI();
        }
    }

    public class BaseCI
    {
        [JsonPropertyName("required_trait")]
        public string RequiredTrait { get; set; }

        [JsonPropertyName("input_whitelist")]
        public List<string> InputWhitelist { get; set; }

        [JsonPropertyName("input_blacklist")]
        public List<string> InputBlacklist { get; set; }

        [JsonPropertyName("attribute_mapping")]
        public List<AttributeMapping> AttributeMapping { get; set; }

        [JsonPropertyName("follow_relations")]
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
        [JsonPropertyName("follow")]
        public List<Follow> Follow { get; set; }

        public FollowRelation()
        {
            Follow = new List<Follow>();
        }
    }

    public class Follow
    {
        [JsonPropertyName("predicate")]
        public string Predicate { get; set; }

        [JsonPropertyName("required_trait")]
        public string RequiredTrait { get; set; }

        [JsonPropertyName("input_blacklist")]
        public List<string> InputBlacklist { get; set; }

        [JsonPropertyName("input_whitelist")]
        public List<string> InputWhitelist { get; set; }

        [JsonPropertyName("attribute_mapping")]
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
        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("target")]
        public string Target { get; set; }

        public AttributeMapping()
        {
            Source = "*"; // NOTE this mean that all attributes should be taken into account.
            Target = "";
        }
    }
}
