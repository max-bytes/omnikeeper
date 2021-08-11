using GraphQL.Types;
using GraphQL.Utilities;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class LayerType : ObjectGraphType<Layer>
    {
        public LayerType()
        {
            Field(x => x.Description);
            Field("brainName", x => x.ComputeLayerBrainLink.Name);
            Field("onlineInboundAdapterName", x => x.OnlineInboundAdapterLink.AdapterName);
            Field("id", x => x.ID);
            Field("color", x => x.Color.ToArgb());
            Field(x => x.State, type: typeof(AnchorStateType));
            Field<BooleanGraphType>("writable",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var lbas = context.RequestServices.GetRequiredService<ILayerBasedAuthorizationService>();
                var isWritable = lbas.CanUserWriteToLayer(userContext.User, context.Source.ID);
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


    public class LayerStatisticsType : ObjectGraphType<LayerStatistics>
    {
        public LayerStatisticsType()
        {
            Field("numActiveAttributes", x => x.NumActiveAttributes);
            Field("numAttributeChangesHistory", x => x.NumAttributeChangesHistory);
            Field("numActiveRelations", x => x.NumActiveRelations);
            Field("numRelationChangesHistory", x => x.NumRelationChangesHistory);
            Field("numLayerChangesetsHistory", x => x.NumLayerChangesetsHistory);
            Field("layer", x => x.Layer, type: typeof(LayerType));
        }
    }
}
