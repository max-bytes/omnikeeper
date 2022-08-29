using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterManager
    {
        Task<string?> ApplyPreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs);
        Task<string?> ApplyPostFilterForMutation(MutationOperation operation, AuthenticatedUser user, IChangesetProxy changesetProxy);

        Task<string?> ApplyPreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs);
    }

    public static class AuthzFilterManagerExtensions
    {
        public static async Task<string?> ApplyPreFilterForMutation(this IAuthzFilterManager manager, MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID)
        {
            return await manager.ApplyPreFilterForMutation(operation, user, readLayerIDs, new string[] { writeLayerID });
        }
        public static async Task<string?> ApplyPreFilterForMutation(this IAuthzFilterManager manager, MutationOperation operation, AuthenticatedUser user, string readLayerID, string writeLayerID)
        {
            return await manager.ApplyPreFilterForMutation(operation, user, new string[] { readLayerID }, new string[] { writeLayerID });
        }

        public static async Task<string?> ApplyPreFilterForQuery(this IAuthzFilterManager manager, QueryOperation operation, AuthenticatedUser user, string readLayerID)
        {
            return await manager.ApplyPreFilterForQuery(operation, user, new string[] { readLayerID });
        }
    }
}
