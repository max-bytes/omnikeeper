using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils.ModelContext
{
    public class ModelContextBuilder : IModelContextBuilder
    {
        private readonly IDistributedCache? memoryCache;
        private readonly NpgsqlConnection npgsqlConnection;
        private readonly ILogger<IModelContext> logger;

        public ModelContextBuilder(IDistributedCache? memoryCache, NpgsqlConnection npgsqlConnection, ILogger<IModelContext> logger)
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
        public IModelContext BuildDeferred(IsolationLevel isolationLevel)
        {
            var npgsqlTransaction = npgsqlConnection.BeginTransaction(isolationLevel);
            return new ModelContextDeferredMode(npgsqlTransaction, memoryCache, logger);
        }

        public IModelContext BuildImmediate()
        {
            return new ModelContextImmediateMode(memoryCache, npgsqlConnection, logger);
        }
    }

    public class ModelContextImmediateMode : IModelContext
    {
        private readonly IDistributedCache? cache;
        private readonly NpgsqlConnection conn;
        private readonly ILogger<IModelContext> logger;

        public ModelContextImmediateMode(IDistributedCache? cache, NpgsqlConnection conn, ILogger<IModelContext> logger)
        {
            this.cache = cache;
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

        public void ClearCache()
        {
            if (cache != null)
            {
                cache.Clear();
            }
        }

        public async Task<(TItem item, bool hit)> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory) where TItem : class
        {
            if (cache != null)
            {
                return await cache.GetOrCreateThreadSafeAsync(key, factory);
            }
            else
            {
                var item = await factory();
                return (item, false);
            }
        }

        public bool TryGetCachedValue<TItem>(string cacheKey, [MaybeNull] out TItem? cacheValue) where TItem : class
        {
            if (cache != null)
            {
                return cache.TryGetValue(cacheKey, out cacheValue);
            }
            else
            {
                cacheValue = default;
                return false;
            }
        }

        public void SetCacheValue<TItem>(string cacheKey, TItem cacheValue) where TItem : class
        {
            if (cache != null)
            {
                cache.SetValue(cacheKey, cacheValue);
            }
        }

        public void EvictFromCache(string key)
        {
            if (cache != null)
            {
                cache.RemoveValue(key);
            }
        }
    }


    public class ModelContextDeferredMode : IModelContext
    {
        private readonly IDistributedCache? memoryCache;
        private readonly ILogger<IModelContext> logger;
        private readonly ISet<string> cacheEvictions = new HashSet<string>();
        private bool cacheClearFlag = false;

        public ModelContextDeferredMode(NpgsqlTransaction dbTransaction, IDistributedCache? memoryCache, ILogger<IModelContext> logger)
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
            // TODO: need a distributed lock here
            DBTransaction.Commit();
            if (memoryCache != null)
            {
                if (cacheClearFlag)
                    memoryCache.Clear();
                else
                    foreach (var ce in cacheEvictions) // TODO: consider having special method for removing multiples at once
                        memoryCache.RemoveValue(ce);
            }
            cacheEvictions.Clear();
            cacheClearFlag = false;
        }

        public void Dispose()
        {
            DBTransaction.Dispose();
            cacheEvictions.Clear();
        }

        public void Rollback()
        {
            DBTransaction.Rollback();
            cacheEvictions.Clear();
            cacheClearFlag = false;
        }

        public void EvictFromCache(string key)
        {
            cacheEvictions.Add(key);
        }

        public void ClearCache()
        {
            cacheClearFlag = true;
        }

        public async Task<(TItem item, bool hit)> GetOrCreateCachedValueAsync<TItem>(string key, Func<Task<TItem>> factory) where TItem : class
        {
            // we cannot use the cache at all in the deferred case, because it could return items that are not reflecting the changes in the transaction so far
            // instead, we even need to add the key to the ones that need to be evicted on commit, because it might already be set and hence it needs to be updated
            cacheEvictions.Add(key);
            return (await factory(), false);
        }

        public bool TryGetCachedValue<TItem>(string cacheKey, [MaybeNull] out TItem cacheValue) where TItem : class
        {
            // we cannot use the cache at all in the deferred case, because it could return items that are not reflecting the changes in the transaction so far
            cacheValue = default;
            return false;
        }

        public void SetCacheValue<TItem>(string cacheKey, TItem cacheValue) where TItem : class
        {
            //  we cannot use cache at all
            // instead, we even need to add the key to the ones that need to be evicted on commit, because it might already be set and hence it needs to be updated
            cacheEvictions.Add(cacheKey);
        }
    }


    public static class DistributedCacheExtensions
    {
        public static async Task<(T item, bool hit)> GetOrCreateThreadSafeAsync<T>(this IDistributedCache memoryCache, string key, Func<Task<T>> factory) where T : class
        {
            // try to avoid lock by first checking if the key is already present (which should be the case most of the time)
            var bytes = memoryCache.Get(key);
            T? r;
            if (bytes != null)
            {
                r = FromByteArray<T>(bytes);
                if (r != null)
                    return (r, true);
            }

            r = await factory(); // TODO: move inside lock?
            lock (TypeLock<T>.Lock) // TODO: this should be a distributed lock
            {
                var b = ToByteArray(r);
                memoryCache.Set(key, b);
                return (r, false);
            }
        }

        internal static void SetValue<TItem>(this IDistributedCache memoryCache, string cacheKey, TItem cacheValue) where TItem : class
        {
            var b = ToByteArray(cacheValue);
            memoryCache.Set(cacheKey, b);
        }

        public static TItem? GetValue<TItem>(this IDistributedCache memoryCache, string key) where TItem : class
        {
            var bytes = memoryCache.Get(key);
            TItem? r = null;
            if (bytes != null)
            {
                r = FromByteArray<TItem>(bytes);
            }
            return r;
        }
        internal static bool TryGetValue<TItem>(this IDistributedCache memoryCache, string key, out TItem? cacheValue) where TItem : class
        {
            var bytes = memoryCache.Get(key);
            TItem? r = null;
            if (bytes != null)
            {
                r = FromByteArray<TItem>(bytes);
            }
            if (r != null)
            {
                cacheValue = r;
                return true;
            }
            cacheValue = null;
            return false;
        }

        internal static void RemoveValue(this IDistributedCache memoryCache, string key)
        {
            memoryCache.Remove(key); // TODO: async without waiting better?
        }

        private static byte[] ToByteArray(object obj)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }

        private static T? FromByteArray<T>(byte[] byteArray) where T : class
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using MemoryStream memoryStream = new MemoryStream(byteArray);
            return binaryFormatter.Deserialize(memoryStream) as T;
        }

        private static class TypeLock<T> where T : class
        {
            public static object Lock { get; } = new object();
        }

        public static void Clear(this IDistributedCache cache)
        {
            if (cache is MemoryDistributedCache mdc)
            {
                // HACK: IDistributedCache interface sucks, so we write our own method to clear the cache based on reflection 
                var memcacheField = typeof(MemoryDistributedCache).GetField("_memCache", BindingFlags.NonPublic | BindingFlags.Instance);
                var memcache = (MemoryCache)memcacheField!.GetValue(mdc)!;
                memcache.Compact(1.0);
            }
            else
            {
                throw new Exception("Clearing cache not supported");
            }
        }
    }


    public static class MemoryCacheHelper
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
