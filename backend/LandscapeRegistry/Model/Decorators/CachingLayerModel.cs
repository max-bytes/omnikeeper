using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingLayerModel : ILayerModel
    {
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<CachingLayerModel> logger;

        private ILayerModel Model { get; }

        public CachingLayerModel(ILayerModel model, IMemoryCache cache, ILogger<CachingLayerModel> logger)
        {
            Model = model;
            memoryCache = cache;
            this.logger = logger;
        }

        public async Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.LayerSet(layerNames), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.BuildLayerSet(layerNames, trans);
            });
        }
        public async Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.AllLayersSet(), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.BuildLayerSet(trans);
            });
        }

        public async Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.LayerById(layerID), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.GetLayer(layerID, trans);
            });
        }

        public async Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.LayerByName(layerName), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.GetLayer(layerName, trans);
            });
        }

        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.LayersByIDs(layerIDs), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.GetLayers(layerIDs, trans);
            });
        }

        public async Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.AllLayers(), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.GetLayers(trans);
            });
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.LayersByStateFilter(stateFilter), async (ce) =>
            {
                var changeToken = memoryCache.GetLayersCancellationChangeToken(logger);
                ce.AddExpirationToken(changeToken);
                return await Model.GetLayers(stateFilter, trans);
            });
        }

        public async Task<bool> TryToDelete(long id, NpgsqlTransaction trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            if (succeeded)
            {
                CacheKeyService.CancelLayersChangeTokens(memoryCache, logger);
            }
            return succeeded;
        }

        public async Task<Layer> CreateLayer(string name, NpgsqlTransaction trans)
        {
            var layer = await Model.CreateLayer(name, trans);
            if (layer != null) CacheKeyService.CancelLayersChangeTokens(memoryCache, logger);
            return layer;
        }

        public async Task<Layer> CreateLayer(string name, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, NpgsqlTransaction trans)
        {
            var layer = await Model.CreateLayer(name, color, state, computeLayerBrain, oilp, trans);
            if (layer != null) CacheKeyService.CancelLayersChangeTokens(memoryCache, logger);
            return layer;
        }

        public async Task<Layer> Update(long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, NpgsqlTransaction trans)
        {
            var layer = await Model.Update(id, color, state, computeLayerBrain, oilp, trans);
            if (layer != null) CacheKeyService.CancelLayersChangeTokens(memoryCache, logger);
            return layer;
        }
    }
}
