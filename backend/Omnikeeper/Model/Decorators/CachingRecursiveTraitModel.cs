using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingRecursiveTraitModel : IRecursiveTraitModel
    {
        private readonly IRecursiveTraitModel model;

        public CachingRecursiveTraitModel(IRecursiveTraitModel model)
        {
            this.model = model;
        }

        public async Task<RecursiveTraitSet> GetRecursiveTraitSet(IModelContext trans, TimeThreshold timeThreshold)
        {
            if (timeThreshold.IsLatest)
            {
                var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.Traits(), async () =>
                {
                    return await model.GetRecursiveTraitSet(trans, timeThreshold);
                });
                return item;
            }
            else return await model.GetRecursiveTraitSet(trans, timeThreshold);
        }

        public async Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.Traits()); // TODO: only evict cache when insert changes
            return await model.SetRecursiveTraitSet(traitSet, trans);
        }
    }
}
