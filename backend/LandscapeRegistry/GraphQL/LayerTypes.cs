using GraphQL.Types;
using Landscape.Base.Entity;
using System.Linq;

namespace LandscapeRegistry.GraphQL
{
    public class LayerType : ObjectGraphType<Layer>
    {
        public LayerType()
        {
            Field(x => x.Name);
            Field("brainName", x => x.ComputeLayerBrainLink.Name);
            Field("onlineInboundAdapterName", x => x.OnlineInboundAdapterLink.AdapterName);
            Field("id", x => x.ID);
            Field("color", x => x.Color.ToArgb());
            Field(x => x.State, type: typeof(AnchorStateType));
            Field<BooleanGraphType>("writable",
            resolve: (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;
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
