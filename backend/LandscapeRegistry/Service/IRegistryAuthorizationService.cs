using Landscape.Base.Entity;
using System.Collections.Generic;

namespace LandscapeRegistry.Service
{
    public interface IRegistryAuthorizationService
    {
        string GetWriteAccessRoleNameFromLayerName(string layerName);
        string ParseLayerNameFromWriteAccessRoleName(string roleName);

        bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer);

        bool CanUserWriteToLayer(AuthenticatedUser user, long layerID);

        bool CanUserWriteToLayers(AuthenticatedUser user, IEnumerable<long> writeLayerIDs);

        bool CanUserCreateCI(AuthenticatedUser user);

        bool CanUserCreateLayer(AuthenticatedUser user);

        bool CanUserUpdateLayer(AuthenticatedUser user);

        bool CanUserUpsertPredicate(AuthenticatedUser user);

        bool CanUserUpsertCIType(AuthenticatedUser user);
    }
}