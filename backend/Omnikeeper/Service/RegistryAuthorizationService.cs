using Omnikeeper.Base.Entity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Omnikeeper.Service
{
    // TODO: rename to OmnikeeperAuthorizationService and IOmnikeeperAuthorizationService
    public class RegistryAuthorizationService : IRegistryAuthorizationService
    {
        private static readonly string ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX = "layer_writeaccess_";
        private readonly bool debugAllowAll;

        public RegistryAuthorizationService(IConfiguration configuration)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
        }

        public string GetWriteAccessRoleNameFromLayerName(string layerName)
        {
            return $"{ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX}{layerName}"; // TODO: define allowed characters
        }

        public string ParseLayerNameFromWriteAccessRoleName(string roleName)
        {
            var match = Regex.Match(roleName, "^layer_writeaccess_(.*)");
            if (!match.Success) return null;
            var layerName = match.Groups[1];
            return layerName.Value;
        }

        public bool CanUserWriteToLayer(AuthenticatedUser user, Layer layer)
        {
            return debugAllowAll || user.WritableLayers.Contains(layer);
        }

        public bool CanUserWriteToLayer(AuthenticatedUser user, long layerID)
        {
            return debugAllowAll || user.WritableLayers.Any(l => l.ID == layerID);
        }

        public bool CanUserWriteToLayers(AuthenticatedUser user, IEnumerable<long> writeLayerIDs)
        {
            // writeLayerIDs must be subset of writable layers, otherwise user can't write to all passed layers
            return debugAllowAll || !writeLayerIDs.Except(user.WritableLayers.Select(l => l.ID)).Any();
        }

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
