using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingLayerModel : ILayerModel
    {
        private readonly ILogger<CachingLayerModel> logger;

        private ILayerModel Model { get; }

        public CachingLayerModel(ILayerModel model, ILogger<CachingLayerModel> logger)
        {
            Model = model;
            this.logger = logger;
        }

        public async Task<LayerSet> BuildLayerSet(string[] layerNames, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.LayerSet(layerNames), 
                async () => await Model.BuildLayerSet(layerNames, trans), CacheKeyService.LayersChangeToken());
        }
        public async Task<LayerSet> BuildLayerSet(IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersSet(), 
                async () => await Model.BuildLayerSet(trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<Layer> GetLayer(long layerID, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.LayerById(layerID), 
                async () => await Model.GetLayer(layerID, trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<Layer> GetLayer(string layerName, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.LayerByName(layerName), 
                async () => await Model.GetLayer(layerName, trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.LayersByIDs(layerIDs),
                async () => await Model.GetLayers(layerIDs, trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayers(),
                async () => await Model.GetLayers(trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.LayersByStateFilter(stateFilter),
                async () => await Model.GetLayers(stateFilter, trans), CacheKeyService.LayersChangeToken());
        }

        public async Task<bool> TryToDelete(long id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            if (succeeded)
            {
                trans.CancelToken(CacheKeyService.LayersChangeToken());
            }
            return succeeded;
        }

        public async Task<Layer> CreateLayer(string name, IModelContext trans)
        {
            var layer = await Model.CreateLayer(name, trans);
            trans.CancelToken(CacheKeyService.LayersChangeToken());
            return layer;
        }

        public async Task<Layer> CreateLayer(string name, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans)
        {
            var layer = await Model.CreateLayer(name, color, state, computeLayerBrain, oilp, trans);
            trans.CancelToken(CacheKeyService.LayersChangeToken());
            return layer;
        }

        public async Task<Layer> Update(long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans)
        {
            var layer = await Model.Update(id, color, state, computeLayerBrain, oilp, trans);
            trans.CancelToken(CacheKeyService.LayersChangeToken());
            return layer;
        }
    }
}
