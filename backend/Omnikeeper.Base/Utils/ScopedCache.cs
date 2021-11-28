using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static async Task<T?> GetFromScopedCache<ConcreteScopedCache>(ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger logger, Func<Task<T>> factory) where ConcreteScopedCache : ScopedCache<T>
        {
            var cache = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<ConcreteScopedCache>();
            if (cache == null)
            {
                logger.LogDebug("Cannot use per request cache because we are not in a scoped lifetime context");
                return null;
            }
            return await cache.GetOrCreate(logger, factory);
        }

        public static void ClearScopedCache<ConcreteScopedCache>(ScopedLifetimeAccessor scopedLifetimeAccessor, ILogger logger) where ConcreteScopedCache : ScopedCache<T>
        {
            var cache = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<ConcreteScopedCache>();
            if (cache != null)
            {
                logger.LogDebug("Clearing cache");
                cache.ClearCache();
            }
        }
    }
}
