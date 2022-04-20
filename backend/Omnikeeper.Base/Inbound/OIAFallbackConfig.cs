using System.Text.Json.Serialization;
using static Omnikeeper.Base.Inbound.IOnlineInboundAdapter;

namespace Omnikeeper.Base.Inbound
{
    public struct OIAFallbackConfig : IConfig
    {
        [JsonIgnore]
        public string BuilderName => "No Builder For Fallback";

        public string MapperScope => "FallbackScope";

        public readonly string unparsableConfig;

        public OIAFallbackConfig(string unparsableConfig)
        {
            this.unparsableConfig = unparsableConfig;
        }
    }
}
