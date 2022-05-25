using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Service;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Utils
{
    public class ScopedCache<T> where T : class
    {
        private T? cached = null;

        public async Task<T> GetOrCreate(ILogger logger, Func<Task<T>> factory)
        {
            if (cached != null)
            {
                logger.LogDebug("Cache hit");
                return cached;
            }
            else
            {
                logger.LogDebug("Cache miss");
                cached = await factory();
                return cached;
            }
        }

        public void ClearCache()
        {
            cached = null;
        }

        public static async Task<T> GetFromScopedCache<ConcreteScopedCache>(ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger logger, Func<Task<T>> factory) where ConcreteScopedCache : ScopedCache<T>
        {
            var lifetimeScope = scopedLifetimeAccessor.GetLifetimeScope();
            if (lifetimeScope == null)
            {
                logger.LogError("Cannot use per request cache because we are not in a scoped lifetime context");
                throw new Exception("Cannot use per request cache because we are not in a scoped lifetime context");
            }

            if (lifetimeScope.TryResolve<ConcreteScopedCache>(out var cache))
            {
                return await cache.GetOrCreate(logger, factory);
            }
            else
            {
                throw new Exception("No concrete scoped cache registered");
            }
        }

        public static void ClearScopedCache<ConcreteScopedCache>(ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger logger) where ConcreteScopedCache : ScopedCache<T>
        {
            var lifetimeScope = scopedLifetimeAccessor.GetLifetimeScope();
            if (lifetimeScope != null)
            {
                if (lifetimeScope.TryResolve<ConcreteScopedCache>(out var cache))
                {
                    logger.LogDebug("Clearing cache");
                    cache.ClearCache();
                }
                else
                {
                    throw new Exception("No concrete scoped cache registered");
                }
            }
        }
    }
}
