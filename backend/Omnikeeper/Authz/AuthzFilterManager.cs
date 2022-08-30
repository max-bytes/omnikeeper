using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Authz
{
    public class AuthzFilterManager : IAuthzFilterManager
    {
        private readonly IEnumerable<IAuthzFilterForMutation> filtersMutation;
        private readonly IEnumerable<IAuthzFilterForQuery> filtersQuery;

        public AuthzFilterManager(IEnumerable<IAuthzFilterForMutation> filtersMutation, IEnumerable<IAuthzFilterForQuery> filtersQuery)
        {
            this.filtersMutation = filtersMutation;
            this.filtersQuery = filtersQuery;
        }

        public async Task<IAuthzFilterResult> ApplyPreFilterForMutationCIs(MutationOperationCIs operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans)
        {
            // NOTE: we do not run any authz filters if the user is a super user
            // otherwise, a filter would be able to forbid the super user to do any action, which we don't want
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PreFilterForMutationCIs(operation, user, readLayerIDs, writeLayerIDs, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPostFilterForMutationCIs(MutationOperationCIs operation, AuthenticatedUser user, IChangesetProxy changesetProxy, IModelContext trans)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PostFilterForMutationCIs(operation, user, changesetProxy, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }



        public async Task<IAuthzFilterResult> ApplyPreFilterForMutationTraitEntities(MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans)
        {
            // NOTE: we do not run any authz filters if the user is a super user
            // otherwise, a filter would be able to forbid the super user to do any action, which we don't want
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PreFilterForMutationTraitEntities(operation, trait, user, readLayerIDs, writeLayerIDs, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPostFilterForMutationTraitEntities(MutationOperationTraitEntities operation, ITrait trait, AuthenticatedUser user, Changeset? changeset, IModelContext trans)
        {
            if (changeset == null) // no changeset -> no change -> permit
                return AuthzFilterResultPermit.Instance;
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PostFilterForMutationTraitEntities(operation, trait, user, changeset, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }


        public async Task<IAuthzFilterResult> ApplyPreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersQuery)
            {
                var r = await filter.PreFilterForQuery(operation, user, readLayerIDs, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }
    }
}
