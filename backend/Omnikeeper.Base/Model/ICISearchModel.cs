using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold timeThreshold);
    }
}
