using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IAttributeModel : IBaseAttributeMutationModel
    {
        Task<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> GetMergedAttributes(ICIIDSelection cs, IAttributeSelection attributeSelection, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        // NOTE: we may be able to remove this, it is just a convenience wrapper over GetMergedAttributes()
        Task<IDictionary<Guid, MergedCIAttribute>> FindMergedAttributesByFullName(string name, ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        /**
         * NOTE: this does not return an entry for CIs that do not have a name specified; in other words, it can return less than specified via the selection parameter
         */
        Task<IDictionary<Guid, string>> GetMergedCINames(ICIIDSelection selection, LayerSet layers, IModelContext trans, TimeThreshold atTime);

        Task<MergedCIAttribute?> GetFullBinaryMergedAttribute(string name, Guid ciid, LayerSet layerset, IModelContext trans, TimeThreshold atTime);

    }
}
