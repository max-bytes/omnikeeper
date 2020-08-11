using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRelationModel : IBaseRelationModel
    {
        Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
