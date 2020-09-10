using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingTraitModel : ITraitModel
    {
        private readonly ITraitModel model;
        private readonly IMemoryCache memoryCache;

        public CachingTraitModel(ITraitModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<TraitSet> GetTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            if (timeThreshold.IsLatest)
                return await memoryCache.GetOrCreateAsync(CacheKeyService.Traits(), async (ce) =>
                {
                    var changeToken = memoryCache.GetTraitsCancellationChangeToken();
                    ce.AddExpirationToken(changeToken);
                    return await model.GetTraitSet(trans, timeThreshold);
                });
            else return await model.GetTraitSet(trans, timeThreshold);
        }

        public async Task<TraitSet> SetTraitSet(TraitSet traitSet, NpgsqlTransaction trans)
        {
            memoryCache.CancelTraitsChangeToken(); // TODO: only evict cache when insert changes
            return await model.SetTraitSet(traitSet, trans);
        }
    }
}
