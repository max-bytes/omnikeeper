using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterManager
    {
        Task<IAuthzFilterResult> ApplyPreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans);
        Task<IAuthzFilterResult> ApplyPostFilterForMutation(MutationOperation operation, AuthenticatedUser user, IChangesetProxy changesetProxy, IModelContext trans);

        Task<IAuthzFilterResult> ApplyPreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans);
    }

    public static class AuthzFilterManagerExtensions
    {
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutation(this IAuthzFilterManager manager, MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutation(operation, user, readLayerIDs, new string[] { writeLayerID }, trans);
        }
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutation(this IAuthzFilterManager manager, MutationOperation operation, AuthenticatedUser user, string readLayerID, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutation(operation, user, new string[] { readLayerID }, new string[] { writeLayerID }, trans);
        }

        public static async Task<IAuthzFilterResult> ApplyPreFilterForQuery(this IAuthzFilterManager manager, QueryOperation operation, AuthenticatedUser user, string readLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForQuery(operation, user, new string[] { readLayerID }, trans);
        }
    }
}
