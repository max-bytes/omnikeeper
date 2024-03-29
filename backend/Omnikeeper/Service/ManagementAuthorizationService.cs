﻿using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Authz;
using Omnikeeper.Authz;

namespace Omnikeeper.Service
{
    public class ManagementAuthorizationService : IManagementAuthorizationService
    {
        private readonly bool debugAllowAll;
        private readonly ILayerBasedAuthorizationService lbas;
        private readonly IAuthRolePermissionChecker authRolePermissionChecker;

        public ManagementAuthorizationService(IConfiguration configuration, ILayerBasedAuthorizationService lbas, IAuthRolePermissionChecker authRolePermissionChecker)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
            this.lbas = lbas;
            this.authRolePermissionChecker = authRolePermissionChecker;
        }

        public bool HasManagementPermission(IAuthenticatedUser user)
        {
            if (debugAllowAll)
                return true;

            if (user is AuthenticatedInternalUser)
                return true;
            else if (user is not AuthenticatedHttpUser ahu)
                throw new System.Exception("Invalid user");
            else
            {
                foreach (var ar in ahu.AuthRoles)
                {
                    if (authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetManagementPermission()))
                        return true;
                }
                return false;
            }
        }

        public bool CanReadManagement(IAuthenticatedUser user, MetaConfiguration metaConfiguration, out string message)
        {
            if (!HasManagementPermission(user))
            {
                message = $"User \"{user.Username}\" does not have management permission";
                return false;
            }

            if (!lbas.CanUserReadFromAllLayers(user, metaConfiguration.ConfigLayerset))
            {
                message = $"User \"{user.Username}\" does not have permission to read from at least one of the configuration layers {string.Join(", ", metaConfiguration.ConfigLayerset)}";
                return false;
            }

            message = "";
            return true;
        }

        public bool CanModifyManagement(IAuthenticatedUser user, MetaConfiguration metaConfiguration, out string message)
        {
            if (!HasManagementPermission(user))
            {
                message = $"User \"{user.Username}\" does not have management permission";
                return false;
            }

            if (!lbas.CanUserWriteToLayer(user, metaConfiguration.ConfigWriteLayer))
            {
                message = $"User \"{user.Username}\" does not have permission to write to layer {metaConfiguration.ConfigWriteLayer}";
                return false;
            }

            if (!lbas.CanUserReadFromAllLayers(user, metaConfiguration.ConfigLayerset))
            {
                message = $"User \"{user.Username}\" does not have permission to read from at least one of the configuration layers {string.Join(", ", metaConfiguration.ConfigLayerset)}";
                return false;
            }

            message = "";
            return true;
        }
    }
}
