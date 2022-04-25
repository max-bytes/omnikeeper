
using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Load
{
    public class LoadConfigTypeDiscriminatorConverter : TypeDiscriminatorConverter<ILoadConfig>
    {
        public LoadConfigTypeDiscriminatorConverter() : base("$type", typeof(LoadConfigTypeDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(LoadConfigTypeDiscriminatorConverter))]
    public interface ILoadConfig
    {
        string[] SearchLayerIDs { get; }
        string WriteLayerID { get; }
        public string type { get; }
    }

    public class LoadConfig : ILoadConfig
    {
        public LoadConfig(string[] searchLayerIDs, string writeLayerID)
        {
            SearchLayerIDs = searchLayerIDs;
            WriteLayerID = writeLayerID;
        }

        public string[] SearchLayerIDs { get; }
        public string WriteLayerID { get; }

        [JsonPropertyName("$type")]
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());
    }
}
