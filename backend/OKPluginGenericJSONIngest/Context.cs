using Newtonsoft.Json;
using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Load;
using OKPluginGenericJSONIngest.Transform;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Entity;
using System.Text.RegularExpressions;

namespace OKPluginGenericJSONIngest
{
    [TraitEntity("__meta.config.gji_context", TraitOriginType.Plugin)]
    public class Context : TraitEntity
    {
        [TraitAttribute("id", "gji_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(ContextIDRegexString, ContextIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("extract_config", "gji_context.extract_config", isJSONSerialized: true)]
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly IExtractConfig ExtractConfig;

        [TraitAttribute("transform_config", "gji_context.transform_config", isJSONSerialized: true)]
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ITransformConfig TransformConfig;

        [TraitAttribute("load_config", "gji_context.load_config", isJSONSerialized: true)]
        [JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
        public readonly ILoadConfig LoadConfig;

        [TraitAttribute("name", "__name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        public Context()
        {
            ID = "";
            ExtractConfig = new ExtractConfigPassiveRESTFiles();
            TransformConfig = new TransformConfigJMESPath("");
            LoadConfig = new LoadConfig(new string[0], "");
            Name = "";
        }

        [JsonConstructor]
        public Context(string id, IExtractConfig extractConfig, ITransformConfig transformConfig, ILoadConfig loadConfig)
        {
            ID = id;
            ExtractConfig = extractConfig;
            TransformConfig = transformConfig;
            LoadConfig = loadConfig;
            Name = $"GridView-Context {ID}";
        }

        public const string ContextIDRegexString = "^[a-z0-9_]+$";
        public const RegexOptions ContextIDRegexOptions = RegexOptions.None;
        public static Regex ContextIDRegex = new Regex(ContextIDRegexString, ContextIDRegexOptions);
    }
}
