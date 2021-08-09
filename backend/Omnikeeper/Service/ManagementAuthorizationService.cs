using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;

namespace Omnikeeper.Service
{
    public class ManagementAuthorizationService : IManagementAuthorizationService
    {
        private readonly bool debugAllowAll;

        public ManagementAuthorizationService(IConfiguration configuration)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
        }

        public bool HasManagementPermission(AuthenticatedUser user)
        {
            return debugAllowAll || user.Permissions.Contains(PermissionUtils.GetManagementPermission());
        }
    }
}
