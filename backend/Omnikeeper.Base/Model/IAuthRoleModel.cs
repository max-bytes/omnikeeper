using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IAuthRoleModel
    {
        Task<IDictionary<string, AuthRole>> GetAuthRoles(IModelContext trans, TimeThreshold atTime);
        Task<AuthRole> GetAuthRole(string id, TimeThreshold atTime, IModelContext trans);
        Task<(Guid, AuthRole)> TryToGetAuthRole(string id, TimeThreshold timeThreshold, IModelContext trans);
    }

}
