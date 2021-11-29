using Npgsql;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class LatestLayerChangeCache
    {
        private readonly IDictionary<string, DateTimeOffset?> cache = new Dictionary<string, DateTimeOffset?>();

        public void UpdateCache(string layerID, DateTimeOffset? latestChange)
        {
            cache[layerID] = latestChange;
        }

        public void RemoveFromCache(string layerID)
        {
            cache.Remove(layerID);
        }

        public bool TryGetValue(string layerID, [MaybeNullWhen(false)] out DateTimeOffset? v)
        {
            return cache.TryGetValue(layerID, out v);
        }
    }

    public class CachingLatestLayerChangeModel : ILatestLayerChangeModel
    {
        private readonly ILatestLayerChangeModel model;
        private readonly LatestLayerChangeCache cache;

        public CachingLatestLayerChangeModel(ILatestLayerChangeModel model, LatestLayerChangeCache cache)
        {
            this.model = model;
            this.cache = cache;
        }

        public async Task<DateTimeOffset?> GetLatestChangeInLayer(string layerID, IModelContext trans)
        {
            if (cache.TryGetValue(layerID, out var v))
            {
                return v;
            }
            else
            {
                var d = await model.GetLatestChangeInLayer(layerID, trans);

                cache.UpdateCache(layerID, d);

                return d;
            }
        }
    }
}
