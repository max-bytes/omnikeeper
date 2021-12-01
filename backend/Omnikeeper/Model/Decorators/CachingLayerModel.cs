using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public async Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.BuildLayerSet(ids, trans);

            var selectedLayerIDs = ids.Select(id =>
            {
                if (allLayers.TryGetValue(id, out var l))
                    return l.ID;
                else
                    throw new Exception(@$"Could not find layer with name ""{id}""");
            });

            return new LayerSet(selectedLayerIDs);
        }

        public async Task<Layer?> GetLayer(string layerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayer(layerID, trans, timeThreshold);

            if (allLayers.TryGetValue(layerID, out var l))
                return l;
            else
                return null;
        }

        public async Task<IEnumerable<Layer>> GetLayers(IEnumerable<string> layerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayers(layerIDs, trans, timeThreshold);

            var selectedLayers = layerIDs.Select(id =>
            {
                if (allLayers.TryGetValue(id, out var l))
                    return l;
                else
                    return null;
            }).WhereNotNull();

            return selectedLayers;
        }

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayers(trans, timeThreshold);

            return allLayers.Values;
        }

        public async Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, IModelContext trans, TimeThreshold timeThreshold)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayers(stateFilter, trans, timeThreshold);

            var selectedLayers = allLayers.Values.Where(layer =>
            {
                return stateFilter.Filter2States().Contains(layer.State);

            });

            return selectedLayers;
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            _ClearCache();
            return succeeded;
        }

        public async Task<Layer> UpsertLayer(string id, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, trans);
            _ClearCache();
            return layer;
        }

        public async Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, string clConfigID, OnlineInboundAdapterLink oilp, string[] generators, IModelContext trans)
        {
            var layer = await Model.UpsertLayer(id, description, color, state, clConfigID, oilp, generators, trans);
            _ClearCache();
            return layer;
        }

        private async Task<IDictionary<string, Layer>?> _GetFromCache(IModelContext trans)
        {
            return await PerRequestLayerCache.GetFromScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger, async () => (await Model.GetLayers(trans, TimeThreshold.BuildLatest())).ToDictionary(l => l.ID));
        }

        private void _ClearCache()
        {
            PerRequestLayerCache.ClearScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger);
        }
    }
}

