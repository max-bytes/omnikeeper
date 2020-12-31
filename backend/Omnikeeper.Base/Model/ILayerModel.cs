using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] layerNames, IModelContext trans);

        Task<Layer> CreateLayer(string name, IModelContext trans);
        Task<Layer> CreateLayer(string name, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans);
        Task<Layer> Update(long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans);
        Task<bool> TryToDelete(long id, IModelContext trans);

        Task<Layer?> GetLayer(long layerID, IModelContext trans);
        Task<Layer?> GetLayer(string layerName, IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans);
    }
}
