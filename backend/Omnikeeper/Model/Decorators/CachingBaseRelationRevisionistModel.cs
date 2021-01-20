using Omnikeeper.Base.Model;
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

        public async Task<int> DeleteAllRelations(long layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllRelations(layerID, trans);
            if (numDeleted > 0)
                trans.ClearCache(); // NOTE, HACK, TODO: we'd like to be more specific here, but cache does not support that
            return numDeleted;
        }
    }
}
