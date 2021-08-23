using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;

namespace Omnikeeper.Base.Service
{
    public interface IManagementAuthorizationService
    {
        bool HasManagementPermission(AuthenticatedUser user);
        bool CanReadManagement(AuthenticatedUser user, BaseConfigurationV1 baseConfiguration, out string message);
        bool CanModifyManagement(AuthenticatedUser user, BaseConfigurationV1 baseConfiguration, out string message);
    }
}