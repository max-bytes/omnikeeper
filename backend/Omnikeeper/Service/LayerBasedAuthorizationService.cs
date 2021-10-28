using Microsoft.Extensions.Configuration;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Service
{
    public class LayerBasedAuthorizationService : ILayerBasedAuthorizationService
    {
        private readonly bool debugAllowAll;
        private readonly IUsageTrackingService usageTrackingService;

        public LayerBasedAuthorizationService(IConfiguration configuration, IUsageTrackingService usageTrackingService)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
            this.usageTrackingService = usageTrackingService;
        }

        public bool CanUserReadFromLayer(AuthenticatedUser user, Layer layer) => CanUserReadFromLayer(user, layer.ID);
        public bool CanUserReadFromLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanReadFromLayers(user.AuthRoles, user.Username, new string[] { layerID });
        public bool CanUserReadFromAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanReadFromLayers(user.AuthRoles, user.Username, layerIDs);

        public bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer) => CanUserWriteToLayer(user, layer.ID);
        public bool CanUserWriteToLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanWriteToLayers(user.AuthRoles, user.Username, new string[] { layerID });

        public bool CanUserWriteToAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || CanWriteToLayers(user.AuthRoles, user.Username, layerIDs);

        private bool CanReadFromLayers(AuthRole[] authRoles, string username, IEnumerable<string> layerIDs)
        {
            var toCheckLayerIDs = new List<string>(layerIDs);
            foreach (var ar in authRoles)
            {
                for(int i = toCheckLayerIDs.Count - 1;i >= 0;i--)
                {
                    var layerID = toCheckLayerIDs[i];
                    if (ar.Permissions.Contains(PermissionUtils.GetLayerReadPermission(layerID)))
                    {
                        usageTrackingService.TrackUseAuthRole(ar.ID, username);
                        toCheckLayerIDs.RemoveAt(i);
                    }
                }

                if (toCheckLayerIDs.IsEmpty())
                    return true;
            }
            return false;
        }
        private bool CanWriteToLayers(AuthRole[] authRoles, string username, IEnumerable<string> layerIDs)
        {
            var toCheckLayerIDs = new List<string>(layerIDs);
            var relevantAuthRoles = new HashSet<AuthRole>();
            for (int i = toCheckLayerIDs.Count - 1; i >= 0; i--)
            {
                var layerID = toCheckLayerIDs[i];
                var readAllowed = false;
                var writeAllowed = false;
                foreach (var ar in authRoles)
                {
                    if (!readAllowed && ar.Permissions.Contains(PermissionUtils.GetLayerReadPermission(layerID)))
                    {
                        relevantAuthRoles.Add(ar);
                        readAllowed = true;
                    }

                    if (!writeAllowed && ar.Permissions.Contains(PermissionUtils.GetLayerWritePermission(layerID)))
                    {
                        relevantAuthRoles.Add(ar);
                        writeAllowed = true;
                    }
                }
                // writing to a layer also requires read permissions
                if (!readAllowed || !writeAllowed)
                    break;
                else
                    toCheckLayerIDs.RemoveAt(i);
            }

            foreach(var ar in relevantAuthRoles)
                usageTrackingService.TrackUseAuthRole(ar.ID, username);

            if (toCheckLayerIDs.IsEmpty())
                return true;

            return false;
        }
    }
}
