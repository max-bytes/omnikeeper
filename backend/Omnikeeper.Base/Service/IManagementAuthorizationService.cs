using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.Config;

namespace Omnikeeper.Base.Service
{
    public interface IManagementAuthorizationService
    {
        bool HasManagementPermission(IAuthenticatedUser user);
        bool CanReadManagement(IAuthenticatedUser user, MetaConfiguration metaConfiguration, out string message);
        bool CanModifyManagement(IAuthenticatedUser user, MetaConfiguration metaConfiguration, out string message);
    }
}