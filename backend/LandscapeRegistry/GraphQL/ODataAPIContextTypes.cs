using GraphQL.Types;
using Landscape.Base.Entity;
using Newtonsoft.Json;

namespace LandscapeRegistry.GraphQL
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
