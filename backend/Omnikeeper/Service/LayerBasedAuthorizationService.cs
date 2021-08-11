using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Service
{
    public class LayerBasedAuthorizationService : ILayerBasedAuthorizationService
    {
        private readonly bool debugAllowAll;

        public LayerBasedAuthorizationService(IConfiguration configuration)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
        }


        public bool CanUserReadFromLayer(AuthenticatedUser user, Layer layer) => CanUserReadFromLayer(user, layer.ID);
        public bool CanUserReadFromLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanReadFromLayer(user.Permissions, layerID);
        public bool CanUserReadFromAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || layerIDs.All(l => CanReadFromLayer(user.Permissions, l));

        public bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer) => CanUserWriteToLayer(user, layer.ID);
        public bool CanUserWriteToLayer(AuthenticatedUser user, string layerID) => debugAllowAll || CanWriteToLayer(user.Permissions, layerID);

        public bool CanUserWriteToAllLayers(AuthenticatedUser user, IEnumerable<string> layerIDs) =>
            debugAllowAll || layerIDs.All(l => CanWriteToLayer(user.Permissions, l));


        private bool CanReadFromLayer(ISet<string> permissions, string layerID)
        {
            return permissions.Contains(PermissionUtils.GetLayerReadPermission(layerID));
        }
        private bool CanWriteToLayer(ISet<string> permissions, string layerID)
        {
            // writing to a layer also requires read permissions
            return permissions.Contains(PermissionUtils.GetLayerReadPermission(layerID)) && permissions.Contains(PermissionUtils.GetLayerWritePermission(layerID));
        }
    }
}
