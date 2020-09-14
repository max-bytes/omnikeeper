using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IPredicateModel
    {
        Task<Predicate> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, NpgsqlTransaction trans, DateTimeOffset? timestamp = null);
        Task<bool> TryToDelete(string id, NpgsqlTransaction trans);
        Task<IDictionary<string, Predicate>> GetPredicates(NpgsqlTransaction trans, TimeThreshold atTime, AnchorStateFilter stateFilter);
    }
}
