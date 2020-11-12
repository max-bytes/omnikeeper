using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingODataAPIContextModel : IODataAPIContextModel
    {
        private IODataAPIContextModel Model { get; }

        public CachingODataAPIContextModel(IODataAPIContextModel model)
        {
            Model = model;
        }

        public async Task<IEnumerable<ODataAPIContext>> GetContexts(IModelContext trans)
        {
            // TODO: caching
            return await Model.GetContexts(trans);
        }

        public async Task<ODataAPIContext?> GetContextByID(string id, IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.ODataAPIContext(id), async () =>
            {
                return await Model.GetContextByID(id, trans);
            }, CacheKeyService.ODataAPIContextChangeToken(id));
        }

        public async Task<ODataAPIContext?> Upsert(string id, ODataAPIContext.IConfig config, IModelContext trans)
        {
            trans.CancelToken(CacheKeyService.ODataAPIContextChangeToken(id));
            return await Model.Upsert(id, config, trans);
        }

        public async Task<ODataAPIContext?> Delete(string id, IModelContext trans)
        {
            var c = await Model.Delete(id, trans);
            if (c != null)
                trans.CancelToken(CacheKeyService.ODataAPIContextChangeToken(c.ID));
            return c;
        }
    }
}
