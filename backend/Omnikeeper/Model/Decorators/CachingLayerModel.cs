using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
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

        public async Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold)
        {
            var allLayers = await _GetFromCache(trans);
            if (allLayers == null)
                return await Model.GetLayers(trans, timeThreshold);

            return allLayers.Values;
        }

        public async Task<bool> TryToDelete(string id, IModelContext trans)
        {
            var succeeded = await Model.TryToDelete(id, trans);
            _ClearCache();
            return succeeded;
        }

        private async Task<IDictionary<string, Layer>?> _GetFromCache(IModelContext trans)
        {
            return await PerRequestLayerCache.GetFromScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger, async () => (await Model.GetLayers(trans, TimeThreshold.BuildLatest())).ToDictionary(l => l.ID));
        }

        private void _ClearCache()
        {
            PerRequestLayerCache.ClearScopedCache<PerRequestLayerCache>(scopedLifetimeAccessor, logger);
        }

        public async Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans)
        {
            var t = await Model.CreateLayerIfNotExists(id, trans);
            if (t.created)
                _ClearCache();
            return t;
        }

        //public async Task<(LayerData layerData, bool changed)> UpsertLayerData(string id, string description, Color color, AnchorState state, string clConfigID, string oiaReference, string[] generators, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        //{
        //    var t = await Model.UpsertLayerData(id, description, color, state, clConfigID, oiaReference, generators, dataOrigin, changesetProxy, trans);
        //    if (t.changed) 
        //        _ClearCache();
        //    return t;
        //}
    }
}

