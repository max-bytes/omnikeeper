using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Inbound;

namespace LandscapeRegistry.GraphQL
{
    public class OIAConfigType : ObjectGraphType<OIAConfig>
    {
        public OIAConfigType()
        {
            Field("id", x => x.ID);
            Field(x => x.Name);
            Field("config", x => IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(x.Config), type: typeof(StringGraphType));
        }
    }
}
