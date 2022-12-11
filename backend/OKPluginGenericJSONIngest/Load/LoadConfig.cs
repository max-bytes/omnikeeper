
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Load
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(LoadConfig), typeDiscriminator: "OKPluginGenericJSONIngest.Load.LoadConfig, OKPluginGenericJSONIngest")]
    public interface ILoadConfig
    {
        string[] SearchLayerIDs { get; }
        string WriteLayerID { get; }
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
    }
}
