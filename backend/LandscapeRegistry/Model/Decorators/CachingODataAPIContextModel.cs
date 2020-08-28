using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Service;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model.Decorators
{
    public class CachingODataAPIContextModel : IODataAPIContextModel
    {
        private readonly IMemoryCache memoryCache;

        private IODataAPIContextModel Model { get; }

        public CachingODataAPIContextModel(IODataAPIContextModel model, IMemoryCache memoryCache)
        {
            Model = model;
            this.memoryCache = memoryCache;
        }

        public async Task<IEnumerable<ODataAPIContext>> GetContexts(NpgsqlTransaction trans)
        {
            // TODO: caching
            return await Model.GetContexts(trans);
        }

        public async Task<ODataAPIContext> GetContextByID(string id, NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync(CacheKeyService.ODataAPIContext(id), async (ce) =>
            {
                var changeToken = memoryCache.GetODataAPIContextCancellationChangeToken(id);
                ce.AddExpirationToken(changeToken);
                return await Model.GetContextByID(id, trans);
            });
        }

        public async Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, NpgsqlTransaction trans)
        {
            memoryCache.CancelODataAPIContextChangeToken(id);
            return await Model.Upsert(id, config, trans);
        }

        public async Task<ODataAPIContext> Delete(string id, NpgsqlTransaction trans)
        {
            var c = await Model.Delete(id, trans);
            if (c != null)
                memoryCache.CancelODataAPIContextChangeToken(c.ID);
            return c;
        }
    }
}
