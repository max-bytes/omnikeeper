using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Cached
{
    public class CachedPredicateModel : IPredicateModel
    {
        private IPredicateModel Model { get; }

        private IDictionary<string, Predicate> PredicateCacheForLatestTime = null;

        private IDictionary<DateTimeOffset, IDictionary<string, Predicate>> AllPredicatesCache = new Dictionary<DateTimeOffset, IDictionary<string, Predicate>>();

        public CachedPredicateModel(IPredicateModel model)
        {
            Model = model;
        }

        public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter)
        {
            IDictionary<string, Predicate> value;
            if (!atTime.IsLatest)
                AllPredicatesCache.TryGetValue(atTime.Time, out value);
            else
                value = PredicateCacheForLatestTime;
            if (value == null)
            {
                value = await Model.GetPredicates(trans, atTime, stateFilter);
                if (!atTime.IsLatest)
                    AllPredicatesCache.Add(atTime.Time, value);
                else
                    PredicateCacheForLatestTime = value;
            }
            return value;
        }

        public async Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, NpgsqlTransaction trans)
        {
            //TODO: add to cache(?)
            return await Model.InsertOrUpdate(id, wordingFrom, wordingTo, state, trans);
        }

        public async Task<bool> TryToDelete(string id, NpgsqlTransaction trans)
        {
            // TODO: remove from cache(?)
            return await Model.TryToDelete(id, trans);
        }
    }
}
