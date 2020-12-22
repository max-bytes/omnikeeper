using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IPredicateModel
    {
        Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, AnchorState state, PredicateConstraints constraints, IModelContext trans, DateTimeOffset? timestamp = null);
        Task<bool> TryToDelete(string id, IModelContext trans);
        Task<IDictionary<string, Predicate>> GetPredicates(IModelContext trans, TimeThreshold atTime, AnchorStateFilter stateFilter);
        Task<Predicate> GetPredicate(string id, TimeThreshold atTime, AnchorStateFilter stateFilter, IModelContext trans);
    }
}
