using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IPredicateWriteService
    {
        Task<(Predicate predicate, bool changed)> InsertOrUpdate(string id, string wordingFrom, string wordingTo, PredicateConstraints constraints, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
        Task<bool> TryToDelete(string id, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
    }
}
