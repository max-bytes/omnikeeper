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
    public class CachedTraitModel : ITraitModel
    {
        private readonly ITraitModel traitModel;
        private readonly IMemoryCache memoryCache;

        public CachedTraitModel(ITraitModel traitModel, IMemoryCache memoryCache)
        {
            this.traitModel = traitModel;
            this.memoryCache = memoryCache;
        }

        public async Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            if (atTime.IsLatest)
            {
                return await memoryCache.GetOrCreateAsync(CacheKeyService.EffectiveTraitsOfCI(ci), async (ce) =>
                {
                    var ciChangeToken = memoryCache.GetOrCreateCICancellationChangeToken(ci.ID);
                    ce.AddExpirationToken(ciChangeToken);
                    return await traitModel.CalculateEffectiveTraitSetForCI(ci, trans, atTime);
                });
            }
            else return await traitModel.CalculateEffectiveTraitSetForCI(ci, trans, atTime);
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching
            return await traitModel.CalculateEffectiveTraitSetsForTraitName(traitName, layerSet, trans, atTime);
        }
    }
}
