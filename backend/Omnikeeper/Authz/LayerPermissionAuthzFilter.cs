using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Authz
{
    public class LayerPermissionAuthzFilter : IAuthzFilterForQuery, IAuthzFilterForMutation
    {
        private readonly ILayerBasedAuthorizationService layerBasedAuthorizationService;

        public LayerPermissionAuthzFilter(ILayerBasedAuthorizationService layerBasedAuthorizationService)
        {
            this.layerBasedAuthorizationService = layerBasedAuthorizationService;
        }

        public Task<IAuthzFilterResult> PreFilterForMutation(IPreMutationOperationContext context, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID, IModelContext trans)
        {
            switch (context)
            {
                case IPreMutationOperationContextForCIs _:
                case PreMutationOperationContextForTraitEntities _:
                    return PreFilterForMutation(user, readLayerIDs, writeLayerID);
                default:
                    throw new System.Exception("Unexpected filter context");
            }
        }

        public Task<IAuthzFilterResult> PostFilterForMutation(IPostMutationOperationContext context, AuthenticatedUser user, Changeset? changeset, IModelContext trans) 
            => Task.FromResult<IAuthzFilterResult>(AuthzFilterResultPermit.Instance);

        private Task<IAuthzFilterResult> PreFilterForMutation(AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID)
        {
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, readLayerIDs))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', readLayerIDs)}"));
            if (!layerBasedAuthorizationService.CanUserWriteToLayer(user, writeLayerID))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to write to layerID: {writeLayerID}"));
            return Task.FromResult<IAuthzFilterResult>(AuthzFilterResultPermit.Instance);
        }

        public Task<IAuthzFilterResult> FilterForQuery(IQueryOperationContext context, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans)
        {
            if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(user, readLayerIDs))
                return Task.FromResult<IAuthzFilterResult>(new AuthzFilterResultDeny($"User \"{user.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', readLayerIDs)}"));
            return Task.FromResult<IAuthzFilterResult>(AuthzFilterResultPermit.Instance);
        }
    }
}
