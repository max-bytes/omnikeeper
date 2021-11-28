using Autofac;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Service;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class PerRequestMetaConfigurationCache : ScopedCache<MetaConfiguration> { }

    public class CachingMetaConfigurationModel : IMetaConfigurationModel
    {
        private readonly ILogger<CachingMetaConfigurationModel> logger;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        private IMetaConfigurationModel Model { get; }

        public CachingMetaConfigurationModel(IMetaConfigurationModel model, ILogger<CachingMetaConfigurationModel> logger, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            Model = model;
            this.logger = logger;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<MetaConfiguration> GetConfigOrDefault(IModelContext trans)
        {
            var item = await _GetFromCache(trans);
            if (item == null)
                return await Model.GetConfigOrDefault(trans);
            return item;
        }

        public async Task<MetaConfiguration> SetConfig(MetaConfiguration config, IModelContext trans)
        {
            _ClearCache();
            return await Model.SetConfig(config, trans);
        }

        private async Task<MetaConfiguration?> _GetFromCache(IModelContext trans)
        {
            return await PerRequestMetaConfigurationCache.GetFromScopedCache<PerRequestMetaConfigurationCache>(scopedLifetimeAccessor, logger, async () => await Model.GetConfigOrDefault(trans));
        }

        private void _ClearCache()
        {
            PerRequestMetaConfigurationCache.ClearScopedCache<PerRequestMetaConfigurationCache>(scopedLifetimeAccessor, logger);
        }
    }
}
