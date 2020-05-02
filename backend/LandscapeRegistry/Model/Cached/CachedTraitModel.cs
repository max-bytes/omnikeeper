using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Cached
{
    public class CachedTraitModel : ITraitModel
    {
        private readonly TraitModel traitModel;
        private readonly IMemoryCache memoryCache;

        public CachedTraitModel(TraitModel traitModel, IMemoryCache memoryCache)
        {
            this.traitModel = traitModel;
            this.memoryCache = memoryCache;
        }

        // TODO: think about time aspect
        private string CacheKey(MergedCI ci) => $"traitsOfCI_{ci.ID}";

        public Task<EffectiveTraitSet> CalculateEffectiveTraitSetForCI(MergedCI ci, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching
            return traitModel.CalculateEffectiveTraitSetForCI(ci, trans, atTime);
        }

        public Task<IEnumerable<EffectiveTraitSet>> CalculateEffectiveTraitSetsForTraitName(string traitName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime)
        {
            // TODO: caching
            return traitModel.CalculateEffectiveTraitSetsForTraitName(traitName, layerSet, trans, atTime);
        }
    }
}
