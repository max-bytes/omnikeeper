using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Authz
{
    public class LayerPermissionAuthzFilter : IAuthzFilter
    {
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public LayerPermissionAuthzFilter(ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public Task<IAuthzFilterResult> PreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs)
        {
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, readLayerIDs))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', readLayerIDs)}"));
            if (!layerBasedAuthorizationService.CanUserWriteToAllLayers(user, writeLayerIDs))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to write to at least one of the following layerIDs: {string.Join(',', writeLayerIDs)}"));
            return Task.FromResult<IAuthzFilterResult>(AuthzFilterResultPermit.Instance);
        }

        public Task<IAuthzFilterResult> PreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs)
        {
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, readLayerIDs))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', readLayerIDs)}"));
            return Task.FromResult<IAuthzFilterResult>(AuthzFilterResultPermit.Instance);
        }
    }
}
