using GraphQL.Types;
using Landscape.Base.Entity;
using System.Linq;

namespace LandscapeRegistry.GraphQL
{
    //public class ComputeLayerBrainType : ObjectGraphType<ComputeLayerBrain>
    //{
    //    public ComputeLayerBrainType()
    //    {
    //        Field(x => x.Name);
    //    }
    //}
    public class LayerType : ObjectGraphType<Layer>
    {
        public LayerType()
        {
            Field(x => x.Name);
            Field("brainName", x => x.ComputeLayerBrain.Name);
            Field("id", x => x.ID);
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
