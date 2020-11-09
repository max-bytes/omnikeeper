using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRelationModel : IBaseRelationModel
    {
        Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
        Task<MergedRelation> GetMergedRelation(Guid fromCIID, Guid toCIID, string predicateID, LayerSet layerset, NpgsqlTransaction trans, TimeThreshold atTime);
    }
}
