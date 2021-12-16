using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold)
        {
            return await Model.GetLayers(trans, timeThreshold);
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            cache.RemoveFromCache(id);
            return succeeded;
        }

        public async Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans)
        {
            var t = await Model.CreateLayerIfNotExists(id, trans);
            if (t.created)
                cache.RemoveFromCache(id);
            return t;
        }

        //public async Task<(LayerData layerData, bool changed)> UpsertLayerData(string id, string description, Color color, AnchorState state, string clConfigID, string oiaReference, string[] generators, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        //{
        //    var t = await Model.UpsertLayerData(id, description, color, state, clConfigID, oiaReference, generators, dataOrigin, changesetProxy, trans);
        //    if (t.changed)
        //        cache.RemoveFromCache(id);
        //    return t;
        //}
    }
}

