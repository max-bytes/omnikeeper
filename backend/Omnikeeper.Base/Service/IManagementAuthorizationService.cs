using Omnikeeper.Base.Entity;

namespace Omnikeeper.Base.Service
{
    public interface IManagementAuthorizationService
    {
        bool CanUserCreateCI(AuthenticatedUser user);

        bool CanUserCreateLayer(AuthenticatedUser user);

        bool CanUserUpdateLayer(AuthenticatedUser user);
    }
}