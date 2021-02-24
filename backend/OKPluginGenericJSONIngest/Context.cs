using Newtonsoft.Json;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Utils;

namespace OKPluginGenericJSONIngest
{
    public class Context
    {
        public readonly string Name;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly IExtractConfig ExtractConfig;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ITransformConfig TransformConfig;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ILoadConfig LoadConfig;

        public Context(string name, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig)
        {
            Name = name;
            ExtractConfig = extractConfig;
            TransformConfig = transformConfig;
            LoadConfig = loadConfig;
        }

        public static MyJSONSerializer<IExtractConfig> ExtractConfigSerializer = new MyJSONSerializer<IExtractConfig>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        });
        public static MyJSONSerializer<ITransformConfig> TransformConfigSerializer = new MyJSONSerializer<ITransformConfig>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        });
        public static MyJSONSerializer<ILoadConfig> LoadConfigSerializer = new MyJSONSerializer<ILoadConfig>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        });
    }
}
