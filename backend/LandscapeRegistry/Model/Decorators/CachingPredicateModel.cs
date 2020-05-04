using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingPredicateModel : IPredicateModel
    {
        private readonly IMemoryCache memoryCache;

        private IPredicateModel Model { get; }

        public CachingPredicateModel(IPredicateModel model, IMemoryCache memoryCache)
        {
            Model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.Predicates(atTime), async (ce) =>
            {
                return await Model.GetPredicates(trans, atTime, stateFilter);
            });
        }

        public async Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, NpgsqlTransaction trans)
        {
            memoryCache.Remove(CacheKeyService.PredicatesLatest());
            return await Model.InsertOrUpdate(id, wordingFrom, wordingTo, state, trans);
        }

        public async Task<bool> TryToDelete(string id, NpgsqlTransaction trans)
        {
            memoryCache.Remove(CacheKeyService.PredicatesLatest());
            return await Model.TryToDelete(id, trans);
        }
    }
}
