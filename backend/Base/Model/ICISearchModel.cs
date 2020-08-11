using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<CompactCI>> AdvancedSearch(string searchString, string[] withEffectiveTraits, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> SimpleSearch(string searchString, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<IEnumerable<CompactCI>> FindCIsWithName(string CIName, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold timeThreshold);
    }
}
