using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingMetaConfigurationModel : IMetaConfigurationModel
    {
        private IMetaConfigurationModel Model { get; }

        public CachingMetaConfigurationModel(IMetaConfigurationModel model)
        {
            Model = model;
        }

        public async Task<MetaConfiguration> GetConfigOrDefault(IModelContext trans)
        {
            var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.MetaConfiguration(), async () =>
            {
                return await Model.GetConfigOrDefault(trans);
            });
            return item;
        }

        public async Task<MetaConfiguration> SetConfig(MetaConfiguration config, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.MetaConfiguration());
            return await Model.SetConfig(config, trans);
        }

        public async Task<bool> IsLayerPartOfMetaConfiguration(string layerID, IModelContext trans)
        {
            return await Model.IsLayerPartOfMetaConfiguration(layerID, trans);
        }
    }
}
