using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
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

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans)
        {
            return await Model.GetLayers(trans);
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
                cache.RemoveFromCache(id); // NOTE: a new layer shouldn't have a cache entry anyway, but we stay safe regardless
            return t;
        }
    }
}

