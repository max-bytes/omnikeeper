using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;

namespace OKPluginGenericJSONIngest
{
    public class Context
    {
        public readonly string Name;
        public readonly IExtractConfig ExtractConfig;
        public readonly ITransformConfig TransformConfig;
        public readonly ILoadConfig LoadConfig;

        public Context(string name, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig)
        {
            Name = name;
            ExtractConfig = extractConfig;
            TransformConfig = transformConfig;
            LoadConfig = loadConfig;
        }
    }
}
