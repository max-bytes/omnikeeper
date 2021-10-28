using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Linq;

namespace Omnikeeper.Service
{
    public class ManagementAuthorizationService : IManagementAuthorizationService
    {
        private readonly bool debugAllowAll;
        private readonly ILayerBasedAuthorizationService lbas;

        public ManagementAuthorizationService(IConfiguration configuration, ILayerBasedAuthorizationService lbas)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
            this.lbas = lbas;
        }

        public bool HasManagementPermission(AuthenticatedUser user)
        {
            if (debugAllowAll)
                return true;

            foreach(var ar in user.AuthRoles)
            {
                if (ar.Permissions.Contains(PermissionUtils.GetManagementPermission()))
                    // TODO: count usages
                    return true;
            }
            return false;
        }

        public bool CanReadManagement(AuthenticatedUser user, MetaConfiguration metaConfiguration, out string message)
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

        public bool CanModifyManagement(AuthenticatedUser user, MetaConfiguration metaConfiguration, out string message)
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
