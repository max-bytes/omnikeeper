using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
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

        public async Task<IAuthzFilterResult> ApplyFilterForQuery(IQueryOperationContext context, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersQuery)
            {
                var r = await filter.FilterForQuery(context, user, readLayerIDs, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPreFilterForMutation(IPreMutationOperationContext context, AuthenticatedUser user, IEnumerable<string> readLayerIDs, string writeLayerID, IModelContext trans)
        {
            // NOTE: we do not run any authz filters if the user is a super user
            // otherwise, a filter would be able to forbid the super user to do any action, which we don't want
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PreFilterForMutation(context, user, readLayerIDs, writeLayerID, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPostFilterForMutation(IPostMutationOperationContext context, AuthenticatedUser user, Changeset? changeset, IModelContext trans)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PostFilterForMutation(context, user, changeset, trans);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }
    }
}
