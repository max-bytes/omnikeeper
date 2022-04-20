
using Omnikeeper.Base.Utils;
using System.Text.Json.Serialization;

namespace OKPluginGenericJSONIngest.Transform.JMESPath
{
    public class TransformConfigJMESPath : ITransformConfig
    {
        public readonly string Expression;

        [JsonPropertyName("$type")]
        public string type => SystemTextJSONSerializerMigrationHelper.GetTypeString(GetType());

        public TransformConfigJMESPath(string expression)
        {
            Expression = expression;
        }
    }
}
