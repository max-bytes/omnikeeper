using Npgsql;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils.ModelContext
{ 
    public interface IModelContextBuilder
    {
        IModelContext BuildDeferred();
        IModelContext BuildImmediate();
    }

    public interface IModelContext : IDbTransaction
    {
        NpgsqlTransaction? DBTransaction { get; }
        NpgsqlConnection DBConnection { get; }

        //void CancelToken(string tokenName);
        void EvictFromCache(string key);

        Task<(TItem item, bool hit)> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory) where TItem : class;
        bool TryGetCachedValue<TItem>(string cacheKey, out TItem? cacheValue) where TItem : class;
        void SetCacheValue<TItem>(string cacheKey, TItem cacheValue) where TItem : class;
    }
}
