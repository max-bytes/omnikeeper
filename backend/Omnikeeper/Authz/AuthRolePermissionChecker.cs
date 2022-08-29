using Omnikeeper.Base.Entity;
using System;
using System.Linq;

namespace Omnikeeper.Authz
{
    public interface IAuthRolePermissionChecker
    {
        bool DoesAuthRoleGivePermission(AuthRole ar, string permission);
    }

    public class AuthRolePermissionChecker : IAuthRolePermissionChecker
    {
        public bool DoesAuthRoleGivePermission(AuthRole ar, string permission)
        {
            return ar.Permissions.Contains(permission);
        }
    }
}
