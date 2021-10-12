using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
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

        public async Task<BaseConfigurationV2> GetConfigOrDefault(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.BaseConfiguration(), async () =>
            {
                return await Model.GetConfigOrDefault(layerSet, timeThreshold, trans);
            });
            return item;
        }

        public async Task<BaseConfigurationV2> SetConfig(BaseConfigurationV2 config, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            trans.EvictFromCache(CacheKeyService.BaseConfiguration());
            return await Model.SetConfig(config, layerSet, writeLayerID, dataOrigin, changesetProxy, trans);
        }
    }
}
