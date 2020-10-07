using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Service;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
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

        public async Task<IEnumerable<OIAConfig>> GetConfigs(bool useFallbackConfig, NpgsqlTransaction trans)
        {
            // TODO: caching
            return await Model.GetConfigs(useFallbackConfig, trans);
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
