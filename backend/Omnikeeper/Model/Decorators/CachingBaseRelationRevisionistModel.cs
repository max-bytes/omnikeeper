using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        private readonly IBaseRelationRevisionistModel model;

        public CachingBaseRelationRevisionistModel(IBaseRelationRevisionistModel model)
        {
            this.model = model;
        }

        public async Task<int> DeleteAllRelations(string layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllRelations(layerID, trans);
            if (numDeleted > 0)
                trans.ClearCache(); // NOTE, HACK, TODO: we'd like to be more specific here, but cache does not support that
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
