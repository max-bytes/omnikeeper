using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;

namespace Omnikeeper.Service
{
    public class ManagementAuthorizationService : IManagementAuthorizationService
    {
        public bool CanUserCreateCI(AuthenticatedUser user)
        {
            return true; // TODO
        }

        public bool CanUserCreateLayer(AuthenticatedUser user)
        {
            return true; // TODO
        }

        public bool CanUserUpdateLayer(AuthenticatedUser user)
        {
            return true; // TODO
        }

        public bool CanUserUpsertPredicate(AuthenticatedUser user)
        {
            return true; // TODO
        }

        public bool CanUserUpsertCIType(AuthenticatedUser user)
        {
            return true; // TODO
        }

        // TODO: add missing stubs for various management tasks (OIA, OData, ...)
    }
}
