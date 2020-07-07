using GraphQL.Types;
using Landscape.Base.Entity;
using Newtonsoft.Json;

namespace LandscapeRegistry.GraphQL
{
    public class OIAConfigType : ObjectGraphType<OIAConfig>
    {
        public OIAConfigType()
        {
            Field("id", x => x.ID);
            Field(x => x.Name);
            Field("config", x => JsonConvert.SerializeObject(x.Config, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }), type: typeof(StringGraphType));
        }
    }
}
