using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Service;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingRecursiveTraitModel : IRecursiveTraitModel
    {
        private readonly IRecursiveTraitModel model;
        private readonly IMemoryCache memoryCache;

        public CachingRecursiveTraitModel(IRecursiveTraitModel model, IMemoryCache memoryCache)
        {
            this.model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<RecursiveTraitSet> GetRecursiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            if (timeThreshold.IsLatest)
                return await memoryCache.GetOrCreateAsync(CacheKeyService.Traits(), async (ce) =>
                {
                    var changeToken = memoryCache.GetTraitsCancellationChangeToken();
                    ce.AddExpirationToken(changeToken);
                    return await model.GetRecursiveTraitSet(trans, timeThreshold);
                });
            else return await model.GetRecursiveTraitSet(trans, timeThreshold);
        }

        public async Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, NpgsqlTransaction trans)
        {
            memoryCache.CancelTraitsChangeToken(); // TODO: only evict cache when insert changes
            return await model.SetRecursiveTraitSet(traitSet, trans);
        }
    }
}
