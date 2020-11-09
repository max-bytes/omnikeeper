using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Service;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseConfigurationModel : IBaseConfigurationModel
    {
        private readonly IMemoryCache memoryCache;

        private IBaseConfigurationModel Model { get; }

        public CachingBaseConfigurationModel(IBaseConfigurationModel model, IMemoryCache memoryCache)
        {
            Model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<BaseConfigurationV1> GetConfig(NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.BaseConfiguration(), async (ce) =>
            {
                var changeToken = memoryCache.GetBaseConfigurationCancellationChangeToken();
                ce.AddExpirationToken(changeToken);
                return await Model.GetConfig(trans);
            });
        }

        public async Task<BaseConfigurationV1> GetConfigOrDefault(NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.BaseConfiguration(), async (ce) =>
            {
                var changeToken = memoryCache.GetBaseConfigurationCancellationChangeToken();
                ce.AddExpirationToken(changeToken);
                return await Model.GetConfigOrDefault(trans);
            });
        }

        public async Task<BaseConfigurationV1> SetConfig(BaseConfigurationV1 config, NpgsqlTransaction trans)
        {
            memoryCache.CancelBaseConfigurationChangeToken();
            return await Model.SetConfig(config, trans);
        }
    }
}
