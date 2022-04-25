using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Extract
{
    public class ExtractConfigTypeDiscriminatorConverter : TypeDiscriminatorConverter<IExtractConfig>
    {
        public ExtractConfigTypeDiscriminatorConverter() : base("$type", typeof(ExtractConfigTypeDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(ExtractConfigTypeDiscriminatorConverter))]
    public interface IExtractConfig
    {
        public string type { get; }
    }

    public class ExtractConfigPassiveRESTFiles : IExtractConfig
    {
        [JsonPropertyName("$type")]
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
    }
}
