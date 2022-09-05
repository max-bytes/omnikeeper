using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterManager
    {
        Task<IAuthzFilterResult> ApplyPreFilterForMutation(IPreMutationOperationContext context, IAuthenticatedUser user, LayerSet readLayerIDs, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold);
        Task<IAuthzFilterResult> ApplyPostFilterForMutation(IPostMutationOperationContext context, IAuthenticatedUser user, LayerSet readLayers, Changeset? changeset, IModelContext trans, TimeThreshold timeThreshold);

        Task<IAuthzFilterResult> ApplyFilterForQuery(IQueryOperationContext context, IAuthenticatedUser user, LayerSet readLayerIDs, IModelContext trans, TimeThreshold timeThreshold);
    }

    public static class AuthzFilterManagerExtensions
    {
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutation(this IAuthzFilterManager manager, IPreMutationOperationContext context, IAuthenticatedUser user, string readLayerID, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await manager.ApplyPreFilterForMutation(context, user, new LayerSet(readLayerID), writeLayerID, trans, timeThreshold);
        }

        public static async Task<IAuthzFilterResult> ApplyPostFilterForMutation(this IAuthzFilterManager manager, IPostMutationOperationContext context, IAuthenticatedUser user, string readLayerID, Changeset? changeset, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await manager.ApplyPostFilterForMutation(context, user, new LayerSet(readLayerID), changeset, trans, timeThreshold);
        }

        public static async Task<IAuthzFilterResult> ApplyFilterForQuery(this IAuthzFilterManager manager, IQueryOperationContext context, IAuthenticatedUser user, string readLayerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await manager.ApplyFilterForQuery(context, user, new LayerSet(readLayerID), trans, timeThreshold);
        }

        // nicer methods for graphql usecases
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutation(this IAuthzFilterManager manager, IPreMutationOperationContext context, string writeLayerID, IOmnikeeperUserContext userContext, IEnumerable<object> path)
        {
            return await manager.ApplyPreFilterForMutation(context, userContext.User, userContext.GetLayerSet(path), writeLayerID, userContext.Transaction, userContext.GetTimeThreshold(path));
        }
        public static async Task<IAuthzFilterResult> ApplyPostFilterForMutation(this IAuthzFilterManager manager, IPostMutationOperationContext context, string writeLayerID, IOmnikeeperUserContext userContext, IEnumerable<object> path)
        {
            return await manager.ApplyPostFilterForMutation(context, userContext.User, userContext.GetLayerSet(path), userContext.ChangesetProxy.GetActiveChangeset(writeLayerID), userContext.Transaction, userContext.GetTimeThreshold(path));
        }
    }
}
