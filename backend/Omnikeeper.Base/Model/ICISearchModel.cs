using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<Guid>> FindCIIDsWithCIName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
        Task<IEnumerable<MergedCI>> FindMergedCIsByTraits(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, IEnumerable<ITrait> withEffectiveTraits, IEnumerable<ITrait> withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
    }
}
