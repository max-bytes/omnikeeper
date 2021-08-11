
namespace OKPluginGenericJSONIngest.Load
{
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
