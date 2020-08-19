using Landscape.Base.Entity;
using Landscape.Base.Inbound;
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
    public class CachingOIAConfigModel : IOIAConfigModel
    {
        private readonly IMemoryCache memoryCache;

        private IOIAConfigModel Model { get; }

        public CachingOIAConfigModel(IOIAConfigModel model, IMemoryCache memoryCache)
        {
            Model = model;
            this.memoryCache = memoryCache;
        }

        //public async Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter)
        //{
        //    if (atTime.IsLatest)
        //        return await memoryCache.GetOrCreateAsync(CacheKeyService.Predicates(stateFilter), async (ce) =>
        //        {
        //            var changeToken = memoryCache.GetPredicatesCancellationChangeToken();
        //            ce.AddExpirationToken(changeToken);
        //            return await Model.GetPredicates(trans, atTime, stateFilter);
        //        });
        //    else return await Model.GetPredicates(trans, atTime, stateFilter);
        //}

        //public async Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, NpgsqlTransaction trans)
        //{
        //    memoryCache.CancelPredicatesChangeToken(); // TODO: only evict cache when insert changes
        //    return await Model.InsertOrUpdate(id, wordingFrom, wordingTo, state, constraints, trans);
        //}

        //public async Task<bool> TryToDelete(string id, NpgsqlTransaction trans)
        //{
        //    var success = await Model.TryToDelete(id, trans);
        //    if (success)
        //        memoryCache.CancelPredicatesChangeToken();
        //    return success;
        //}

        public async Task<IEnumerable<OIAConfig>> GetConfigs(NpgsqlTransaction trans)
        {
            // TODO: caching
            return await Model.GetConfigs(trans);
        }

        public async Task<OIAConfig> GetConfigByName(string name, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.OIAConfig(name), async (ce) =>
            {
                var changeToken = memoryCache.GetOIAConfigCancellationChangeToken(name);
                ce.AddExpirationToken(changeToken);
                return await Model.GetConfigByName(name, trans);
            });
        }

        public async Task<OIAConfig> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            memoryCache.CancelOIAConfigChangeToken(name);
            return await Model.Create(name, config, trans);
        }

        public async Task<OIAConfig> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            memoryCache.CancelOIAConfigChangeToken(name);
            return await Model.Update(id, name, config, trans);
        }

        public async Task<OIAConfig> Delete(long iD, NpgsqlTransaction transaction)
        {
            var c = await Model.Delete(iD, transaction);
            if (c != null)
                memoryCache.CancelOIAConfigChangeToken(c.Name);
            return c;
        }
    }
}
