using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingOIAContextModel : IOIAContextModel
    {
        private IOIAContextModel Model { get; }

        public CachingOIAContextModel(IOIAContextModel model)
        {
            Model = model;
        }

        public async Task<IEnumerable<OIAContext>> GetContexts(bool useFallbackConfig, IModelContext trans)
        {
            // TODO: caching
            return await Model.GetContexts(useFallbackConfig, trans);
        }

        public async Task<OIAContext> GetContextByName(string name, IModelContext trans)
        {
            var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.OIAConfig(name), async () =>
            {
                return await Model.GetContextByName(name, trans);
            });
            return item;
        }

        public async Task<OIAContext> Create(string name, IOnlineInboundAdapter.IConfig config, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.OIAConfig(name));
            return await Model.Create(name, config, trans);
        }

        public async Task<OIAContext> Update(long id, string name, IOnlineInboundAdapter.IConfig config, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.OIAConfig(name));
            return await Model.Update(id, name, config, trans);
        }

        public async Task<OIAContext> Delete(long iD, IModelContext trans)
        {
            var c = await Model.Delete(iD, trans);
            trans.EvictFromCache(CacheKeyService.OIAConfig(c.Name));
            return c;
        }
    }
}
