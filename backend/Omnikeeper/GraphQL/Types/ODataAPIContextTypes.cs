using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL.Types
{
    public class ODataAPIContextType : ObjectGraphType<ODataAPIContext>
    {
        public ODataAPIContextType()
        {
            Field("id", x => x.ID);
            Field("config", x => ODataAPIContext.ConfigSerializer.SerializeToString(x.CConfig), type: typeof(StringGraphType));
        }
    }
}
