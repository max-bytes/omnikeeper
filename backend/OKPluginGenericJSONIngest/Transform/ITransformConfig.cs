using OKPluginGenericJSONIngest.Extract;
using OKPluginGenericJSONIngest.Transform.JMESPath;
using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Transform
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(TransformConfigJMESPath), typeDiscriminator: "OKPluginGenericJSONIngest.Transform.JMESPath.TransformConfigJMESPath, OKPluginGenericJSONIngest")]
    public interface ITransformConfig
    {
    }
}
