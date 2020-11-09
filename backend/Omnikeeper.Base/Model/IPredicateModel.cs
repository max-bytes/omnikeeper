using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IPredicateModel
    {
        Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, NpgsqlTransaction trans, DateTimeOffset? timestamp = null);
        Task<bool> TryToDelete(string id, NpgsqlTransaction trans);
        Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter);
        Task<Predicate> GetPredicate(string id, TimeThreshold atTime, AnchorStateFilter stateFilter, NpgsqlTransaction trans);
    }
}
