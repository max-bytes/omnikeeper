using static Landscape.Base.Inbound.IOnlineInboundAdapter;

namespace Landscape.Base.Inbound
{
    public struct OIAFallbackConfig : IConfig
    {
        [Newtonsoft.Json.JsonIgnore]
        public string BuilderName => "No Builder For Fallback";

        public string MapperScope => "FallbackScope";

        public readonly string unparsableConfig;

        public OIAFallbackConfig(string unparsableConfig)
        {
            this.unparsableConfig = unparsableConfig;
        }
    }
}
