using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Extract
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ExtractConfigPassiveRESTFiles), typeDiscriminator: "OKPluginGenericJSONIngest.Extract.ExtractConfigPassiveRESTFiles, OKPluginGenericJSONIngest")]
    public interface IExtractConfig
    {
    }

    public class ExtractConfigPassiveRESTFiles : IExtractConfig
    {
    }
}
