using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;

namespace Omnikeeper.GraphQL.Types
{
    public class OIAContextType : ObjectGraphType<OIAContext>
    {
        public OIAContextType()
        {
            Field("id", x => x.ID);
            Field(x => x.Name);
            Field("config", x => IOnlineInboundAdapter.IConfig.Serializer.SerializeToString(x.Config), type: typeof(StringGraphType));
        }
    }
}
