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

        // TODO: does caching effective traits make sense even?
        // if we gave up caching effective traits, they could become more powerful -> nested relation-requirements, nested traits-requirements
        // caching the underlying structures instead (attributes, relations, ...) we can still keep this feasible
        // have to think about this more...

        public async Task<EffectiveTrait> CalculateEffectiveTraitForCI(MergedCI ci, Trait trait, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching
            return await model.CalculateEffectiveTraitForCI(ci, trait, trans, atTime);
        }

        public async Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            //if (atTime.IsLatest)
            //{
            //    return await memoryCache.GetOrCreateAsync(CacheKeyService.EffectiveTraitsOfCI(ci), async (ce) =>
            //    {
            //        var ciChangeToken = memoryCache.GetCICancellationChangeToken(ci.ID);
            //        ce.AddExpirationToken(ciChangeToken);
            //        return await model.CalculateEffectiveTraitSetForCI(ci, trans, atTime);
            //    });
            //}
            //else 
                return await model.CalculateEffectiveTraitSetForCI(ci, trans, atTime);
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetForCIs(IEnumerable<MergedCI> cis, string[] traitNames, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // we cannot properly cache this it seems, because it would need to be invalidated whenever ANY CI changes
            return await model.CalculateEffectiveTraitSetForCIs(cis, traitNames, trans, atTime);
        }

        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null)
        {
            // we cannot properly cache this it seems, because it would need to be invalidated whenever ANY CI changes
            return await model.CalculateEffectiveTraitSetsForTraitName(traitName, layerSet, trans, atTime, ciFilter);
        }
        public async Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTrait(Trait trait, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime, Func<Guid, bool> ciFilter = null)
        {
            // we cannot properly cache this it seems, because it would need to be invalidated whenever ANY CI changes
            return await model.CalculateEffectiveTraitSetsForTrait(trait, layerSet, trans, atTime, ciFilter);
        }
    }
}
