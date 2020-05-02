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
        Task<IEnumerable<CompactCI>> Search(string searchString, LayerSet layerSet, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
