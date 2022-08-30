using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterManager
    {
        Task<IAuthzFilterResult> ApplyPreFilterForMutationCIs(MutationOperationCIs operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans);
        Task<IAuthzFilterResult> ApplyPostFilterForMutationCIs(MutationOperationCIs operation, AuthenticatedUser user, Changeset? changeset, IModelContext trans);

        Task<IAuthzFilterResult> ApplyPreFilterForMutationTraitEntities(MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans);
        Task<IAuthzFilterResult> ApplyPostFilterForMutationTraitEntities(MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, Changeset? changeset, IModelContext trans);

        Task<IAuthzFilterResult> ApplyPreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans);
    }

    public static class AuthzFilterManagerExtensions
    {
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutationCIs(this IAuthzFilterManager manager, MutationOperationCIs operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutationCIs(operation, user, readLayerIDs, new string[] { writeLayerID }, trans);
        }
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutationCIs(this IAuthzFilterManager manager, MutationOperationCIs operation, AuthenticatedUser user, string readLayerID, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutationCIs(operation, user, new string[] { readLayerID }, new string[] { writeLayerID }, trans);
        }
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutationTraitEntities(this IAuthzFilterManager manager, MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutationTraitEntities(operation, trait, user, readLayerIDs, new string[] { writeLayerID }, trans);
        }
        public static async Task<IAuthzFilterResult> ApplyPreFilterForMutationTraitEntities(this IAuthzFilterManager manager, MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, string readLayerID, string writeLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForMutationTraitEntities(operation, trait, user, new string[] { readLayerID }, new string[] { writeLayerID }, trans);
        }

        public static async Task<IAuthzFilterResult> ApplyPreFilterForQuery(this IAuthzFilterManager manager, QueryOperation operation, AuthenticatedUser user, string readLayerID, IModelContext trans)
        {
            return await manager.ApplyPreFilterForQuery(operation, user, new string[] { readLayerID }, trans);
        }
    }
}
