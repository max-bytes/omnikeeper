using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;

namespace Omnikeeper.GraphQL
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
