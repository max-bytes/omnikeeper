using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class PerRequestLayerCache : ScopedCache<IDictionary<string, Layer>>
    {
    }

    public class CachingLayerModel : ILayerModel
    {
        private readonly ILogger<CachingLayerModel> logger;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        private ILayerModel Model { get; }

        public CachingLayerModel(ILayerModel model, ILogger<CachingLayerModel> logger, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            Model = model;
            this.logger = logger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<IReadOnlyList<Layer>> GetLayers(IModelContext trans)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayers(trans);

            return allLayers.Values.ToList();
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            _ClearCache();
            return succeeded;
        }

        public async Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans)
        {
            var t = await Model.CreateLayerIfNotExists(id, trans);
            if (t.created)
                _ClearCache();
            return t;
        }

        private async Task<IDictionary<string, Layer>?> _GetFromCache(IModelContext trans)
        {
            return await PerRequestLayerCache.GetFromScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger, async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));
        }

        private void _ClearCache()
        {
            PerRequestLayerCache.ClearScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger);
        }
    }
}

