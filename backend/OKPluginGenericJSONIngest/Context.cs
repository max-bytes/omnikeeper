using Newtonsoft.Json;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using Omnikeeper.Base.Utils;
using System;
using System.Text.RegularExpressions;

namespace OKPluginGenericJSONIngest
{
    public class Context
    {
        public readonly string ID;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly IExtractConfig ExtractConfig;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ITransformConfig TransformConfig;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ILoadConfig LoadConfig;

        public Context(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig)
        {
            ID = id;
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

        public static Regex ContextIDRegex = new Regex("^[a-z0-9_]+$");
        public static void ValidateContextIDThrow(string candidateID)
        {
            if (!ValidateContextID(candidateID))
                throw new Exception($"Invalid context ID \"{candidateID}\"");
        }
        public static bool ValidateContextID(string candidateID)
        {
            return ContextIDRegex.IsMatch(candidateID);
        }
    }
}
