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
        Task<IDictionary<string, Predicate>> GetPredicates(IModelContext trans, TimeThreshold atTime);
        Task<Predicate> GetPredicate(string id, TimeThreshold atTime, IModelContext trans);
        Task<(Guid, Predicate)> TryToGetPredicate(string id, TimeThreshold timeThreshold, IModelContext trans);
    }

}
