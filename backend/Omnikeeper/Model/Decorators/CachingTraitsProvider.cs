using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class PerRequestTraitsProviderCache : ScopedCache<IDictionary<string, ITrait>>
    {
    }

    public class CachingTraitsProvider : ITraitsProvider
    {
        private readonly ILogger<CachingTraitsProvider> logger;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        private ITraitsProvider Provider { get; }

        public CachingTraitsProvider(ITraitsProvider provider, ILogger<CachingTraitsProvider> logger, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            Provider = provider;
            this.logger = logger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var all = await _GetFromCache(trans);
            if (all == null)
                return await Provider.GetActiveTraits(trans, timeThreshold);
            return all;
        }

        public async Task<ITrait?> GetActiveTrait(string traitID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var all = await _GetFromCache(trans);
            if (all == null)
                return await Provider.GetActiveTrait(traitID, trans, timeThreshold);
            return all.GetOrWithClass(traitID, null);
        }

        public async Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            var all = await _GetFromCache(trans);
            if (all == null)
                return await Provider.GetActiveTraitsByIDs(IDs, trans, timeThreshold);
            return all.Where(kv => IDs.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<DateTimeOffset?> GetLatestChangeToActiveDataTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: caching?
            return await Provider.GetLatestChangeToActiveDataTraits(trans, timeThreshold);
        }

        private async Task<IDictionary<string, ITrait>?> _GetFromCache(IModelContext trans)
        {
            // TODO: we should not create a timethreshold instance in the factory
            return await PerRequestTraitsProviderCache.GetFromScopedCache<PerRequestTraitsProviderCache>(scopedLifetimeAccessor, logger, async () => await Provider.GetActiveTraits(trans, TimeThreshold.BuildLatest()));
        }

        private void _ClearCache()
        {
            PerRequestTraitsProviderCache.ClearScopedCache<PerRequestTraitsProviderCache>(scopedLifetimeAccessor, logger);
        }
    }
}

