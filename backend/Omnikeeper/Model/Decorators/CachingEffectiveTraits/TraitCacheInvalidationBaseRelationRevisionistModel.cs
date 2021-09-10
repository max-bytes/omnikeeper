using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Utils;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class TraitCacheInvalidationBaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        private readonly IBaseRelationRevisionistModel model;
        private readonly IBaseConfigurationModel baseConfigurationModel;
        private readonly EffectiveTraitCache cache;

        public TraitCacheInvalidationBaseRelationRevisionistModel(IBaseRelationRevisionistModel model, IBaseConfigurationModel baseConfigurationModel, EffectiveTraitCache cache)
        {
            this.model = model;
            this.baseConfigurationModel = baseConfigurationModel;
            this.cache = cache;
        }

        public async Task<int> DeleteAllRelations(string layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllRelations(layerID, trans);
            if (numDeleted > 0)
            {
                if (await baseConfigurationModel.IsLayerPartOfBaseConfiguration(layerID, trans))
                    cache.PurgeAll();
                else
                    cache.PurgeLayer(layerID);
            }
            return numDeleted;
        }

        public async Task<int> DeleteOutdatedRelationsOlderThan(string layerID, IModelContext trans, System.DateTimeOffset threshold, TimeThreshold atTime)
        {
            // NOTE: because this only deletes outdated (=not latest) relations, it does not affect the cache
            var numDeleted = await model.DeleteOutdatedRelationsOlderThan(layerID, trans, threshold, atTime);
            return numDeleted;
        }
    }
}
