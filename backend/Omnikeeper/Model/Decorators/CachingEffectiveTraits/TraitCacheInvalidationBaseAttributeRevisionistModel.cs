using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class TraitCacheInvalidationBaseAttributeRevisionistModel : IBaseAttributeRevisionistModel
    {
        private readonly IBaseAttributeRevisionistModel model;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly EffectiveTraitCache cache;

        public TraitCacheInvalidationBaseAttributeRevisionistModel(IBaseAttributeRevisionistModel model, IBaseConfigurationModel baseConfigurationModel, EffectiveTraitCache cache)
        {
            this.model = model;
            this.baseConfigurationModel = baseConfigurationModel;
            this.cache = cache;
        }

        public async Task<int> DeleteAllAttributes(string layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllAttributes(layerID, trans);
            if (numDeleted > 0)
            {
                if (await baseConfigurationModel.IsLayerPartOfBaseConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.PurgeLayer(layerID);
            }
            return numDeleted;
        }

        public async Task<int> DeleteOutdatedAttributesOlderThan(string layerID, IModelContext trans, System.DateTimeOffset threshold, TimeThreshold atTime)
        {
            // NOTE: because this only deletes outdated (=not latest) attributes, it does not affect the cache
            var numDeleted = await model.DeleteOutdatedAttributesOlderThan(layerID, trans, threshold, atTime);
            return numDeleted;
        }
    }
}
