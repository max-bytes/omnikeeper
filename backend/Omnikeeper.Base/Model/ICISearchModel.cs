using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<CompactCI>> AdvancedSearchForCompactCIs(string searchString, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
        //Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> FindCompactCIsWithName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
        Task<IEnumerable<MergedCI>> SearchForMergedCIsByTraits(ICIIDSelection ciidSelection, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
    }
}
