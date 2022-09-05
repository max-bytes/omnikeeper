using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Authz
{
    public class LayerBasedAuthorizationService : ILayerBasedAuthorizationService
    {
        private readonly bool debugAllowAll;
        private readonly IAuthRolePermissionChecker authRolePermissionChecker;

        public LayerBasedAuthorizationService(IConfiguration configuration, IAuthRolePermissionChecker authRolePermissionChecker)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
            this.authRolePermissionChecker = authRolePermissionChecker;
        }

        public bool CanUserReadFromLayer(IAuthenticatedUser user, string layerID) => debugAllowAll || CanReadFromLayers(user, new string[] { layerID });
        public bool CanUserReadFromAllLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanReadFromLayers(user, layerIDs);

        public bool CanUserWriteToLayer(IAuthenticatedUser user, Layer layer) => CanUserWriteToLayer(user, layer.ID);
        public bool CanUserWriteToLayer(IAuthenticatedUser user, string layerID) => debugAllowAll || CanWriteToLayers(user, new string[] { layerID });

        public bool CanUserWriteToAllLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanWriteToLayers(user, layerIDs);

        public IEnumerable<LayerData> FilterReadableLayers(IAuthenticatedUser user, IEnumerable<LayerData> layers)
        {
            if (user is AuthenticatedInternalUser)
                foreach (var layer in layers) yield return layer;
            else if (user is not AuthenticatedHttpUser ahu)
                throw new System.Exception("Invalid user");
            else
            {
                foreach (var layer in layers)
                    if (ahu.AuthRoles.Any(ar => authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetLayerReadPermission(layer.LayerID))))
                        yield return layer;
            }
        }
        public IEnumerable<LayerData> FilterWritableLayers(IAuthenticatedUser user, IEnumerable<LayerData> layers)
        {
            if (user is AuthenticatedInternalUser)
                foreach (var layer in layers) yield return layer;
            else if (user is not AuthenticatedHttpUser ahu)
                throw new System.Exception("Invalid user");
            else
            {
                foreach (var layer in layers)
                    if (ahu.AuthRoles.Any(ar => authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetLayerWritePermission(layer.LayerID))))
                        yield return layer;
            }
        }

        private bool CanReadFromLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs)
        {
            if (user is AuthenticatedInternalUser)
                return true;
            else if (user is not AuthenticatedHttpUser ahu)
                throw new System.Exception("Invalid user");
            else
            {
                var toCheckLayerIDs = new List<string>(layerIDs);
                foreach (var ar in ahu.AuthRoles)
                {
                    for (int i = toCheckLayerIDs.Count - 1; i >= 0; i--)
                    {
                        var layerID = toCheckLayerIDs[i];
                        if (authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetLayerReadPermission(layerID)))
                        {
                            toCheckLayerIDs.RemoveAt(i);
                        }
                    }

                    if (toCheckLayerIDs.IsEmpty())
                        return true;
                }
                return false;
            }
        }
        private bool CanWriteToLayers(IAuthenticatedUser user, IEnumerable<string> layerIDs)
        {
            if (user is AuthenticatedInternalUser)
                return true;
            else if (user is not AuthenticatedHttpUser ahu)
                throw new System.Exception("Invalid user");
            else
            {
                var toCheckLayerIDs = new List<string>(layerIDs);
                for (int i = toCheckLayerIDs.Count - 1; i >= 0; i--)
                {
                    var layerID = toCheckLayerIDs[i];
                    var readAllowed = false;
                    var writeAllowed = false;
                    foreach (var ar in ahu.AuthRoles)
                    {
                        if (!readAllowed && authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetLayerReadPermission(layerID)))
                        {
                            readAllowed = true;
                        }

                        if (!writeAllowed && authRolePermissionChecker.DoesAuthRoleGivePermission(ar, PermissionUtils.GetLayerWritePermission(layerID)))
                        {
                            writeAllowed = true;
                        }
                    }
                    // writing to a layer also requires read permissions
                    if (!readAllowed || !writeAllowed)
                        break;
                    else
                        toCheckLayerIDs.RemoveAt(i);
                }

                if (toCheckLayerIDs.IsEmpty())
                    return true;

                return false;
            }
        }
    }
}
