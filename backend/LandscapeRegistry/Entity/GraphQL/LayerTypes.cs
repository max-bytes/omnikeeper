using GraphQL.Types;
using Landscape.Base.Entity;
using System.Linq;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class LayerType : ObjectGraphType<Layer>
    {
        public LayerType()
        {
            Field(x => x.Name);
            Field("id", x => x.ID);
            Field<BooleanGraphType>("writable",
            resolve: (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
                var isWritable = userContext.User.WritableLayers.Any(l => l.ID == context.Source.ID);
                return isWritable;
            });
        }
    }

    public class LayerSetType : ObjectGraphType<LayerSet>
    {
        public LayerSetType()
        {
            Field("ids", x => x.LayerIDs);
        }
    }
}
