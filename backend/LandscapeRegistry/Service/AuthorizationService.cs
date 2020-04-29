using Landscape.Base.Entity;
using Landscape.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LandscapeRegistry.Service
{
    public class AuthorizationService
    {
        private static readonly string ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX = "layer_writeaccess_";

        public string GetWriteAccessRoleNameFromLayerName(string layerName)
        {
            return $"{ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX}{layerName}"; // TODO: define allowed characters
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="roleName">Must contain "/" as prefix</param>
        /// <returns></returns>
        public string ParseLayerNameFromWriteAccessRoleName(string roleName)
        {
            var match = Regex.Match(roleName, "^layer_writeaccess_(.*)");
            if (!match.Success) return null;
            var layerName = match.Groups[1];
            return layerName.Value;
        }

        public bool CanUserWriteToLayer(User user, Layer layer)
        {
            return user.WritableLayers.Contains(layer);
        }

        public bool CanUserWriteToLayer(User user, long layerID)
        {
            return user.WritableLayers.Any(l => l.ID == layerID);
        }

        internal bool CanUserWriteToLayers(User user, IEnumerable<long> writeLayerIDs)
        {
            // writeLayerIDs must be subset of writable layers, otherwise user can't write to all passed layers
            return !writeLayerIDs.Except(user.WritableLayers.Select(l => l.ID)).Any();
        }

        internal bool CanUserCreateCI(User user)
        {
            return true; // TODO
        }

        internal bool CanUserCreateLayer(User user)
        {
            return true; // TODO
        }

        internal bool CanUserUpdateLayer(User user)
        {
            return true; // TODO
        }

        internal bool CanUserUpsertPredicate(User user)
        {
            return true; // TODO
        }

        internal bool CanUserUpsertCIType(User user)
        {
            return true; // TODO
        }
    }
}
