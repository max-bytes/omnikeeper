using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICIModel : ICIIDModel
    {
        public static readonly string NameAttribute = "__name";

        // merged
        Task<MergedCI> GetMergedCI(Guid ciid, LayerSet layers, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<MergedCI>> GetMergedCIs(ICIIDSelection selection, LayerSet layers, bool includeEmptyCIs, IAttributeSelection attributeSelection, IModelContext trans, TimeThreshold atTime);

        Guid CreateCIID();

        Task<Guid> CreateCI(Guid id, IModelContext trans);
        Task<Guid> CreateCI(IModelContext trans);
        Task BulkCreateCIs(IEnumerable<Guid> ids, IModelContext trans);
    }
}
