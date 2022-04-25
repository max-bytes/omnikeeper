using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Transform
{
    public class TransformConfigTypeDiscriminatorConverter : TypeDiscriminatorConverter<ITransformConfig>
    {
        public TransformConfigTypeDiscriminatorConverter() : base("$type", typeof(TransformConfigTypeDiscriminatorConverter))
        {
        }
    }

    [JsonConverter(typeof(TransformConfigTypeDiscriminatorConverter))]
    public interface ITransformConfig
    {
        public string type { get; }
    }
}
