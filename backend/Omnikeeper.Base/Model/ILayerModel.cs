using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans);

        Task<Layer> UpsertLayer(string id, IModelContext trans);
        Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, string[] generators, IModelContext trans);
        Task<bool> TryToDelete(string id, IModelContext trans);

        Task<Layer?> GetLayer(string layerID, IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(IEnumerable<string> layerIDs, IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(IModelContext trans);
        Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans);
    }
}
