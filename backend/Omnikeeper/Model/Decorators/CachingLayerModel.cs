using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public async Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans)
        {
            var (allLayers, _) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersByID(), async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));

            var selectedLayerIDs = ids.Select(id =>
            {
                if (allLayers.TryGetValue(id, out var l))
                    return l.ID;
                else
                    throw new Exception(@$"Could not find layer with name ""{id}""");
            });

            return new LayerSet(selectedLayerIDs);
        }

        public async Task<Layer?> GetLayer(string layerID, IModelContext trans)
        {
            var (allLayers, _) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersByID(), async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));

            if (allLayers.TryGetValue(layerID, out var l))
                return l;
            else
                return null;
        }

        public async Task<IEnumerable<Layer>> GetLayers(string[] layerIDs, IModelContext trans)
        {
            var (allLayers, _) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersByID(), async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));

            var selectedLayers = layerIDs.Select(id =>
            {
                if (allLayers.TryGetValue(id, out var l))
                    return l;
                else
                    return null;
            }).WhereNotNull();

            return selectedLayers;
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans)
        {
            var (allLayers, _) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersByID(), async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));

            return allLayers.Values;
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans)
        {
            var (allLayers, _) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.AllLayersByID(), async () => (await Model.GetLayers(trans)).ToDictionary(l => l.ID));

            var selectedLayers = allLayers.Values.Where(layer =>
            {
                return stateFilter.Filter2States().Contains(layer.State);

            });

            return selectedLayers;
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            trans.EvictFromCache(CacheKeyService.AllLayersByID());
            return succeeded;
        }

        public async Task<Layer> UpsertLayer(string id, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, trans);
            trans.EvictFromCache(CacheKeyService.AllLayersByID());
            return layer;
        }

        public async Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, description, color, state, computeLayerBrain, oilp, trans);
            trans.EvictFromCache(CacheKeyService.AllLayersByID());
            return layer;
        }
    }
}

