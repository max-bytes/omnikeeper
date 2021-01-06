using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, string[] withoutEffectiveTraits, LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, IModelContext trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
    }
}
