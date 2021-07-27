using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class LayerBasedAuthorizationService : ILayerBasedAuthorizationService
    {
        private static readonly string ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX = "layer_writeaccess_";
        private readonly bool debugAllowAll;
        private readonly string audience;
        private readonly IKeycloakAuthorizationService keycloakAuthorizationService;

        public LayerBasedAuthorizationService(IConfiguration configuration, IKeycloakAuthorizationService keycloakAuthorizationService)
        {
            debugAllowAll = configuration.GetSection("Authorization").GetValue("debugAllowAll", false);
            audience = configuration.GetSection("Authentication")["Audience"];
            this.keycloakAuthorizationService = keycloakAuthorizationService;
        }

        private string GetWriteAccessRoleNameFromLayerName(string layerName)
        {
            return $"{ROLE_NAME_LAYER_WRITE_ACCESS_PREFIX}{layerName}"; // TODO: define allowed characters
        }

        private string? ParseLayerNameFromWriteAccessRoleName(string roleName)
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

        public async Task<IEnumerable<Layer>> GetWritableLayersForUser(IEnumerable<Claim> claims, ILayerModel layerModel, IModelContext trans)
        {
            var resourceAccessStr = claims.Where(c => c.Type == "resource_access").FirstOrDefault()?.Value;
            var resourceAccess = resourceAccessStr != null ? JObject.Parse(resourceAccessStr) : null;
            var resourceName = audience;
            var clientRoles = resourceAccess?[resourceName]?["roles"]?.Select(tt => tt.Value<string>()).ToArray() ?? new string[] { };

            var writableLayers = new List<Layer>();
            foreach (var role in clientRoles)
            {
                var layerName = ParseLayerNameFromWriteAccessRoleName(role);
                if (layerName != null)
                {
                    var layer = await layerModel.GetLayer(layerName, trans);
                    if (layer != null)
                        writableLayers.Add(layer);
                }
            }
            return writableLayers;
        }

        public async Task<IEnumerable<Layer>> GetReadableLayersForUser(AuthenticatedUser user, ILayerModel layerModel, IModelContext trans) 
        {
            var allLayers = await layerModel.GetLayers(trans);

            var allowedLayers = await keycloakAuthorizationService.CheckPermissions(user, allLayers, (l) => $"ok:layer:{l.Name}#read_layer");

            return allowedLayers;
        }
    }
}
