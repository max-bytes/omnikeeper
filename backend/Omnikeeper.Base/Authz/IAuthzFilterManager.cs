using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterManager
    {
        Task<IAuthzFilterResult> ApplyPreFilterForMutation(IPreMutationOperationContext context, AuthenticatedUser user, LayerSet readLayerIDs, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold);
        Task<IAuthzFilterResult> ApplyPostFilterForMutation(IPostMutationOperationContext context, AuthenticatedUser user, Changeset? changeset, IModelContext trans);

        Task<IAuthzFilterResult> ApplyFilterForQuery(IQueryOperationContext context, AuthenticatedUser user, LayerSet readLayerIDs, IModelContext trans, TimeThreshold timeThreshold);
    }

    public static class AuthzFilterManagerExtensions
    {
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutation(this IAuthzFilterManager manager, IPreMutationOperationContext context, AuthenticatedUser user, string readLayerID, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await manager.ApplyPreFilterForMutation(context, user, new LayerSet(readLayerID), writeLayerID, trans, timeThreshold);
        }

        public static async Task<IAuthzFilterResult> ApplyFilterForQuery(this IAuthzFilterManager manager, IQueryOperationContext context, AuthenticatedUser user, string readLayerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await manager.ApplyFilterForQuery(context, user, new LayerSet(readLayerID), trans, timeThreshold);
        }
    }
}
