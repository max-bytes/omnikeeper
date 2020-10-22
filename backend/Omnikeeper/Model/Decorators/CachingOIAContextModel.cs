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
    public class CachingOIAContextModel : IOIAContextModel
    {
        private readonly IMemoryCache memoryCache;

        private IOIAContextModel Model { get; }

        public CachingOIAContextModel(IOIAContextModel model, IMemoryCache memoryCache)
        {
            Model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<IEnumerable<OIAContext>> GetContexts(bool useFallbackConfig, NpgsqlTransaction trans)
        {
            // TODO: caching
            return await Model.GetContexts(useFallbackConfig, trans);
        }

        public async Task<OIAContext> GetContextByName(string name, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.OIAConfig(name), async (ce) =>
            {
                var changeToken = memoryCache.GetOIAConfigCancellationChangeToken(name);
                ce.AddExpirationToken(changeToken);
                return await Model.GetContextByName(name, trans);
            });
        }

        public async Task<OIAContext> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            memoryCache.CancelOIAConfigChangeToken(name);
            return await Model.Create(name, config, trans);
        }

        public async Task<OIAContext> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            memoryCache.CancelOIAConfigChangeToken(name);
            return await Model.Update(id, name, config, trans);
        }

        public async Task<OIAContext> Delete(long iD, NpgsqlTransaction transaction)
        {
            var c = await Model.Delete(iD, transaction);
            if (c != null)
                memoryCache.CancelOIAConfigChangeToken(c.Name);
            return c;
        }
    }
}
