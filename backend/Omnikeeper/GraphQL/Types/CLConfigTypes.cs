using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL.Types
{
    public class CLConfigType : ObjectGraphType<CLConfigV1>
    {
        public CLConfigType()
        {
            Field("id", x => x.ID);
            Field("clBrainReference", x => x.CLBrainReference);
            Field("clBrainConfig", x => x.CLBrainConfig.RootElement.ToString());
        }
    }
}
