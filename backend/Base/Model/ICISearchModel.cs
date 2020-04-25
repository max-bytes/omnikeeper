using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ICISearchModel
    {
        Task<IEnumerable<MergedCI>> Search(string searchString, LayerSet layerSet, NpgsqlTransaction trans, DateTimeOffset? atTime = null);
    }
}
