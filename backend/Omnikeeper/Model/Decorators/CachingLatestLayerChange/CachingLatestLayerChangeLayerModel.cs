using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingLatestLayerChangeLayerModel : ILayerModel
    {
        private readonly LatestLayerChangeCache cache;

        private ILayerModel Model { get; }

        public CachingLatestLayerChangeLayerModel(ILayerModel model, LatestLayerChangeCache cache)
        {
            Model = model;
            this.cache = cache;
        }

        public async Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans)
        {
            return await Model.BuildLayerSet(ids, trans);
        }

        public async Task<Layer?> GetLayer(string layerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await Model.GetLayer(layerID, trans, timeThreshold);
        }

        public async Task<IEnumerable<Layer>> GetLayers(IEnumerable<string> layerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await Model.GetLayers(layerIDs, trans, timeThreshold);
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold)
        {
            return await Model.GetLayers(trans, timeThreshold);
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await Model.GetLayers(stateFilter, trans, timeThreshold);
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            cache.RemoveFromCache(id);
            return succeeded;
        }

        public async Task<Layer> UpsertLayer(string id, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, trans);
            cache.RemoveFromCache(id);
            return layer;
        }

        public async Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, string clConfigID, OnlineInboundAdapterLink oilp, string[] generators, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, description, color, state, clConfigID, oilp, generators, trans);
            cache.RemoveFromCache(id);
            return layer;
        }
    }
}

