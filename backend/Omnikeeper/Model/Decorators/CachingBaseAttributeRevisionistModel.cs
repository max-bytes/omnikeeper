using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingBaseAttributeRevisionistModel : IBaseAttributeRevisionistModel
    {
        private readonly IBaseAttributeRevisionistModel model;

        public CachingBaseAttributeRevisionistModel(IBaseAttributeRevisionistModel model)
        {
            this.model = model;
        }

        public async Task<int> DeleteAllAttributes(string layerID, IModelContext trans)
        {
            var numDeleted = await model.DeleteAllAttributes(layerID, trans);
            if (numDeleted > 0)
                trans.ClearCache(); // NOTE, HACK, TODO: we'd like to be more specific here, but cache does not support that
            return numDeleted;
        }
    }
}
