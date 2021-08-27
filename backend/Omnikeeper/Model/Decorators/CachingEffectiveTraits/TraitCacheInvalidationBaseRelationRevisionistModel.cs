using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Utils;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators.CachingEffectiveTraits
{
    public class TraitCacheInvalidationBaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        private readonly IBaseRelationRevisionistModel model;
        private readonly EffectiveTraitCache cache;

        public TraitCacheInvalidationBaseRelationRevisionistModel(IBaseRelationRevisionistModel model, EffectiveTraitCache cache)
        {
            this.model = model;
            this.cache = cache;
        }

        public async Task<int> DeleteAllRelations(string layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllRelations(layerID, trans);
            if (numDeleted > 0)
                cache.PurgeLayer(layerID);
            return numDeleted;
        }
    }
}
