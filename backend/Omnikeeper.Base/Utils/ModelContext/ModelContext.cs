using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Npgsql;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils.ModelContext
{
    public class ModelContextBuilder : IModelContextBuilder
    {
        private readonly IMemoryCache? memoryCache;
        private readonly NpgsqlConnection npgsqlConnection;
        private readonly ILogger<IModelContext> logger;

        public ModelContextBuilder(IMemoryCache? memoryCache, NpgsqlConnection npgsqlConnection, ILogger<IModelContext> logger)
        {
            this.memoryCache = memoryCache;
            this.npgsqlConnection = npgsqlConnection;
            this.logger = logger;
        }

        public IModelContext BuildDeferred()
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction();
            return new ModelContextDeferredMode(npgsqlTransaction, memoryCache, logger);
        }
        public IModelContext BuildImmediate()
        {
            return new ModelContextImmediateMode(memoryCache, npgsqlConnection, logger);
        }
    }

    public class ModelContextImmediateMode : IModelContext
    {
        private readonly IMemoryCache? memoryCache;
        private readonly NpgsqlConnection conn;
        private readonly ILogger<IModelContext> logger;

        public ModelContextImmediateMode(IMemoryCache? memoryCache, NpgsqlConnection conn, ILogger<IModelContext> logger)
        {
            this.memoryCache = memoryCache;
            this.conn = conn;
            this.logger = logger;
        }

        public IDbConnection Connection => conn;
        public IsolationLevel IsolationLevel => default;
        public NpgsqlTransaction? DBTransaction { get; } = null;
        public NpgsqlConnection DBConnection => conn;

        public void Commit()
        {
            // NO-OP
        }

        public void Dispose()
        {
        }

        public void Rollback()
        {
            // NO-OP
        }

        public void CancelToken(string tokenName)
        {
            if (memoryCache != null)
                Helper.CancelAndRemoveChangeToken(memoryCache, tokenName, logger);
        }

        public async Task<TItem> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory, params string[] cancellationTokensToAdd)
        {
            if (memoryCache != null)
                return await memoryCache.GetOrCreateAsync(key, async (ce) =>
                {
                    var item = await factory();
                    foreach (var ct in cancellationTokensToAdd)
                        ce.AddExpirationToken(Helper.GetCancellationChangeToken(memoryCache, ct));
                    return item;
                });
            else
            {
                var item = await factory();
                return item;
            }
        }

        public bool TryGetCachedValue<TItem>(string cacheKey, [MaybeNull] out TItem cacheValue)
        {
            if (memoryCache != null)
                return memoryCache.TryGetValue(cacheKey, out cacheValue);
            else
            {
                cacheValue = default;
                return false;
            }
        }

        public void SetCacheValue<TItem>(string cacheKey, TItem cacheValue, string cancellationToken)
        {
            if (memoryCache != null)
            {
                memoryCache.Set(cacheKey, cacheValue, Helper.GetCancellationChangeToken(memoryCache, cancellationToken));
            }
        }

    }


    public class ModelContextDeferredMode : IModelContext
    {
        private readonly IMemoryCache? memoryCache;
        private readonly ILogger<IModelContext> logger;
        private readonly ISet<string> cacheCancellationTokens = new HashSet<string>();

        public ModelContextDeferredMode(NpgsqlTransaction dbTransaction, IMemoryCache? memoryCache, ILogger<IModelContext> logger)
        {
            DBTransaction = dbTransaction;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public IDbConnection Connection => DBTransaction.Connection;
        public IsolationLevel IsolationLevel => DBTransaction.IsolationLevel;
        public NpgsqlTransaction DBTransaction { get; }
        public NpgsqlConnection DBConnection => DBTransaction.Connection;

        public void Commit()
        {
            DBTransaction.Commit();
            if (memoryCache != null)
                Helper.CancelAndRemoveChangeTokens(memoryCache, cacheCancellationTokens);
            cacheCancellationTokens.Clear();
        }

        public void Dispose()
        {
            DBTransaction.Dispose();
            cacheCancellationTokens.Clear();
        }

        public void Rollback()
        {
            DBTransaction.Rollback();
            cacheCancellationTokens.Clear();
        }

        public void CancelToken(string tokenName)
        {
            cacheCancellationTokens.Add(tokenName);
        }

        public async Task<TItem> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory, params string[] cancellationTokensToAdd)
        {
            // we cannot use the cache at all in the deferred case, because it could return items that are not reflecting the changes in the transaction so far
            return await factory();
        }

        public bool TryGetCachedValue<TItem>(string cacheKey, [MaybeNull] out TItem cacheValue)
        {
            // we cannot use the cache at all in the deferred case, because it could return items that are not reflecting the changes in the transaction so far
            cacheValue = default;
            return false;
        }

        public void SetCacheValue<TItem>(string cacheKey, TItem cacheValue, string cancellationToken)
        {
            //  we cannot use cache at all
            // instead, we even need to add the cancellationToken to the ones that need to be cancelled on commit, because it might already be set and hence it needs to be updated
            cacheCancellationTokens.Add(cancellationToken);
        }
    }


    public static class Helper
    {
        /// <summary>
        /// despite some conflicting information on the web, IMemoryCache.GetOrCreate() is NOT(!) properly thread safe. That means that parallel invocations of it can lead to different values getting returned
        /// this function fixes the issue by using a lock
        /// see https://github.com/aspnet/Caching/issues/359 and https://github.com/dotnet/runtime/issues/36499 for the issue
        /// see https://tpodolak.com/blog/2017/12/13/asp-net-core-memorycache-getorcreate-calls-factory-method-multiple-times/ for the base solution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memoryCache"></param>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static T GetOrCreateThreadSafe<T>(IMemoryCache memoryCache, object key, Func<ICacheEntry, T> factory)
        {
            // try to avoid lock by first checking if the key is already present (which should be the case most of the time)
            if (memoryCache.TryGetValue<T>(key, out var result))
            {
                return result;
            }

            lock (TypeLock<T>.Lock)
            {
                return memoryCache.GetOrCreate(key, factory);
            }
        }

        public static CancellationChangeToken GetCancellationChangeToken(IMemoryCache memoryCache, string tokenName)
        {
            var ts = GetOrCreateThreadSafe(memoryCache, tokenName, (ce) =>
            {
                var ts = new CancellationTokenSource();
                return ts;
            });
            return new CancellationChangeToken(ts.Token);
        }

        public static void CancelAndRemoveChangeToken(IMemoryCache memoryCache, string tokenKey, ILogger? logger = null)
        {
            if (logger != null)
                logger.LogDebug($"Cancelling the token source with key {tokenKey}");
            lock (TypeLock<CancellationTokenSource>.Lock)  // Get() and Remove() are not atomic, so we add a lock here
            {
                var tokenSource = memoryCache.Get<CancellationTokenSource>(tokenKey);
                if (tokenSource != null)
                {
                    if (logger != null)
                        logger.LogDebug($"Cancelling the token source with token {tokenSource.GetHashCode()}");
                    memoryCache.Remove(tokenKey);
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                else
                {
                    if (logger != null)
                        logger.LogDebug($"Could not find token source with key {tokenKey}");
                }
            }
        }

        public static void CancelAndRemoveChangeTokens(IMemoryCache memoryCache, IEnumerable<string> tokenKeys)
        {
            lock (TypeLock<CancellationTokenSource>.Lock)  // Get() and Remove() are not atomic, so we add a lock here
            {
                foreach (var tokenKey in tokenKeys)
                {
                    var tokenSource = memoryCache.Get<CancellationTokenSource>(tokenKey);
                    if (tokenSource != null)
                    {
                        memoryCache.Remove(tokenKey);
                        tokenSource.Cancel();
                        tokenSource.Dispose();
                    }
                }
            }
        }

        private static class TypeLock<T>
        {
            public static object Lock { get; } = new object();
        }
    }

}
