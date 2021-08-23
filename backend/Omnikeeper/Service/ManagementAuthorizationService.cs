using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

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
            return debugAllowAll || user.Permissions.Contains(PermissionUtils.GetManagementPermission());
        }

        public bool CanReadManagement(AuthenticatedUser user, BaseConfigurationV1 baseConfiguration, out string message)
        {
            if (!HasManagementPermission(user))
            {
                message = $"User \"{user.Username}\" does not have management permission";
                return false;
            }

            if (!lbas.CanUserReadFromAllLayers(user, baseConfiguration.ConfigLayerset))
            {
                message = $"User \"{user.Username}\" does not have permission to read from at least one of the configuration layers {string.Join(", ", baseConfiguration.ConfigLayerset)}";
                return false;
            }

            message = "";
            return true;
        }

        public bool CanModifyManagement(AuthenticatedUser user, BaseConfigurationV1 baseConfiguration, out string message)
        {
            if (!HasManagementPermission(user))
            {
                message = $"User \"{user.Username}\" does not have management permission";
                return false;
            }

            if (!lbas.CanUserWriteToLayer(user, baseConfiguration.ConfigWriteLayer))
            {
                message = $"User \"{user.Username}\" does not have permission to write to layer {baseConfiguration.ConfigWriteLayer}";
                return false;
            }

            if (!lbas.CanUserReadFromAllLayers(user, baseConfiguration.ConfigLayerset))
            {
                message = $"User \"{user.Username}\" does not have permission to read from at least one of the configuration layers {string.Join(", ", baseConfiguration.ConfigLayerset)}";
                return false;
            }

            message = "";
            return true;
        }
    }
}
