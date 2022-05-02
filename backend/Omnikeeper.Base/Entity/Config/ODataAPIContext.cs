using Omnikeeper.Base.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity
{
    [TraitEntity("__meta.config.odata_context", TraitOriginType.Core)]
    public class ODataAPIContext : TraitEntity
    {
        public class ConfigTypeDiscriminatorConverter : TypeDiscriminatorConverter<IConfig>
        {
            public ConfigTypeDiscriminatorConverter() : base("$type", typeof(ConfigTypeDiscriminatorConverter))
            {
            }
        }

        [JsonConverter(typeof(ConfigTypeDiscriminatorConverter))]
        public interface IConfig
        {
            public string type { get; }
        }

        public class ConfigV3 : IConfig
        {
            public ConfigV3(string writeLayerID, string[] readLayerset)
            {
                WriteLayerID = writeLayerID;
                ReadLayerset = readLayerset;
            }

            [JsonPropertyName("$type")]
            public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

            public string WriteLayerID { get; set; }
            public string[] ReadLayerset { get; set; }
        }

        [TraitAttribute("id", "odata_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("config", "odata_context.config", jsonSerializer: typeof(ConfigSerializer))]
        public readonly IConfig CConfig;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public static SystemTextJSONSerializer<IConfig> ConfigSerializer = new SystemTextJSONSerializer<IConfig>(new JsonSerializerOptions());

        public ODataAPIContext(string iD, IConfig cConfig)
        {
            ID = iD;
            CConfig = cConfig;
            Name = $"OData-Context - {iD}";
        }
        public ODataAPIContext() { 
            ID = ""; 
            CConfig = new ConfigV3("", System.Array.Empty<string>());
            Name = "";
        }
    }

    public class ConfigSerializer : AttributeJSONSerializer<ODataAPIContext.ConfigV3>
    {
        public ConfigSerializer() : base(() =>
        {
            return new JsonSerializerOptions()
            {
                Converters = {
                        new JsonStringEnumConverter()
                    },
                IncludeFields = true
            };
        })
        {
        }
    }

}
