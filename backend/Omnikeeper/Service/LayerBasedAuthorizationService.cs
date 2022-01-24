using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Utils;
using System.Collections.Generic;

namespace Omnikeeper.Service
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

        public bool CanUserReadFromLayer(AuthenticatedUser user, Layer layer) => CanUserReadFromLayer(user, layer.ID);
        public bool CanUserReadFromLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanReadFromLayers(user.AuthRoles, new string[] { layerID });
        public bool CanUserReadFromAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanReadFromLayers(user.AuthRoles, layerIDs);

        public bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer) => CanUserWriteToLayer(user, layer.ID);
        public bool CanUserWriteToLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanWriteToLayers(user.AuthRoles, new string[] { layerID });

        public bool CanUserWriteToAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanWriteToLayers(user.AuthRoles, layerIDs);

        private bool CanReadFromLayers(AuthRole[] authRoles, IEnumerable<string> layerIDs)
        {
            var toCheckLayerIDs = new List<string>(layerIDs);
            foreach (var ar in authRoles)
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
        private bool CanWriteToLayers(AuthRole[] authRoles, IEnumerable<string> layerIDs)
        {
            var toCheckLayerIDs = new List<string>(layerIDs);
            for (int i = toCheckLayerIDs.Count - 1; i >= 0; i--)
            {
                var layerID = toCheckLayerIDs[i];
                var readAllowed = false;
                var writeAllowed = false;
                foreach (var ar in authRoles)
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
