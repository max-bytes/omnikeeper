using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IAuthRoleWriteService
    {
        Task<(AuthRole authRole, bool changed)> InsertOrUpdate(string id, IEnumerable<string> permissions, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
        Task<bool> TryToDelete(string id, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
    }
}
