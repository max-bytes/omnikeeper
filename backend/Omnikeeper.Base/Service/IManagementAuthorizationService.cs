using Omnikeeper.Base.Entity;

namespace Omnikeeper.Base.Service
{
    public interface IManagementAuthorizationService
    {
        bool HasManagementPermission(AuthenticatedUser user);
    }
}