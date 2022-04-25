using Omnikeeper.Base.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity
{
    public class ODataAPIContext
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

        public string ID { get; set; }
        public IConfig CConfig { get; set; }

        public static SystemTextJSONSerializer<IConfig> ConfigSerializer = new SystemTextJSONSerializer<IConfig>(new JsonSerializerOptions());

        public ODataAPIContext(string iD, IConfig cConfig)
        {
            ID = iD;
            CConfig = cConfig;
        }
    }

}
