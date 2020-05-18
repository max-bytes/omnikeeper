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
            if (atTime.IsLatest)
                return await memoryCache.GetOrCreateAsync(CacheKeyService.Predicates(stateFilter), async (ce) =>
                {
                    var changeToken = memoryCache.GetPredicatesCancellationChangeToken();
                    ce.AddExpirationToken(changeToken);
                    return await Model.GetPredicates(trans, atTime, stateFilter);
                });
            else return await Model.GetPredicates(trans, atTime, stateFilter);
        }

        public async Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, NpgsqlTransaction trans)
        {
            memoryCache.CancelPredicatesChangeToken(); // TODO: only evict cache when insert changes
            return await Model.InsertOrUpdate(id, wordingFrom, wordingTo, state, constraints, trans);
        }

        public async Task<bool> TryToDelete(string id, NpgsqlTransaction trans)
        {
            var success = await Model.TryToDelete(id, trans);
            if (success)
                memoryCache.CancelPredicatesChangeToken();
            return success;
        }
    }
}
