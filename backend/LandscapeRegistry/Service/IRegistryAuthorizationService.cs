using Landscape.Base.Entity;
using System.Collections.Generic;

namespace LandscapeRegistry.Service
{
    public interface IRegistryAuthorizationService
    {
        string GetWriteAccessRoleNameFromLayerName(string layerName);
        string ParseLayerNameFromWriteAccessRoleName(string roleName);

        bool CanUserWriteToLayer(User user, Layer layer);

        bool CanUserWriteToLayer(User user, long layerID);

        bool CanUserWriteToLayers(User user, IEnumerable<long> writeLayerIDs);

        bool CanUserCreateCI(User user);

        bool CanUserCreateLayer(User user);

        bool CanUserUpdateLayer(User user);

        bool CanUserUpsertPredicate(User user);

        bool CanUserUpsertCIType(User user);
    }
}