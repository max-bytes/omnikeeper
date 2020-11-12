
namespace Omnikeeper.Base.Entity
{
    public class LayerStatistics
    {
        public LayerStatistics(Layer layer, long numActiveAttributes, long numAttributeChangesHistory, long numActiveRelations, long numRelationChangesHistory, long numLayerChangesetsHistory)
        {
            Layer = layer;
            NumActiveAttributes = numActiveAttributes;
            NumAttributeChangesHistory = numAttributeChangesHistory;
            NumActiveRelations = numActiveRelations;
            NumRelationChangesHistory = numRelationChangesHistory;
            NumLayerChangesetsHistory = numLayerChangesetsHistory;
        }

        public Layer Layer { get; private set; }
        public long NumActiveAttributes { get; private set; }
        public long NumAttributeChangesHistory { get; private set; }
        public long NumActiveRelations { get; private set; }
        public long NumRelationChangesHistory { get; private set; }
        public long NumLayerChangesetsHistory { get; private set; }

    }
}
