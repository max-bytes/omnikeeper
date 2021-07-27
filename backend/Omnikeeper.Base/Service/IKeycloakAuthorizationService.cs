using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IKeycloakAuthorizationService
    {
        Task<bool> HasPermission(AuthenticatedUser user, string permission);
        Task<bool> HasPermissions(AuthenticatedUser user, ISet<string> permissions);

        Task<ISet<string>> CheckPermissions(AuthenticatedUser user, ISet<string> permissions);
        Task<IEnumerable<T>> CheckPermissions<T>(AuthenticatedUser user, IEnumerable<T> objects, Func<T, string> permissionExtractor);
    }
}
