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
        void CancelToken(string tokenName);

        Task<TItem> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory, params string[] cancellationTokensToAdd);
        bool TryGetCachedValue<TItem>(string cacheKey, out TItem cacheValue);
        void SetCacheValue<TItem>(string cacheKey, TItem cacheValue, string cancellationToken);
    }
}
