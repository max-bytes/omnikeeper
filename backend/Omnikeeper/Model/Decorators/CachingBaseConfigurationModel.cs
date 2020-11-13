using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseConfigurationModel : IBaseConfigurationModel
    {
        private IBaseConfigurationModel Model { get; }

        public CachingBaseConfigurationModel(IBaseConfigurationModel model)
        {
            Model = model;
        }

        public async Task<BaseConfigurationV1?> GetConfig(IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.BaseConfiguration(), async () =>
            {
                return await Model.GetConfig(trans);
            }, CacheKeyService.BaseConfigurationChangeToken());
        }

        public async Task<BaseConfigurationV1> GetConfigOrDefault(IModelContext trans)
        {
            return await trans.GetOrCreateCachedValueAsync(CacheKeyService.BaseConfiguration(), async () =>
            {
                return await Model.GetConfigOrDefault(trans);
            }, CacheKeyService.BaseConfigurationChangeToken());
        }

        public async Task<BaseConfigurationV1> SetConfig(BaseConfigurationV1 config, IModelContext trans)
        {
            trans.CancelToken(CacheKeyService.BaseConfigurationChangeToken());
            return await Model.SetConfig(config, trans);
        }
    }
}
