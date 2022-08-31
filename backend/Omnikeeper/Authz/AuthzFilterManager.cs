﻿using Omnikeeper.Base.Authz;
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

        public async Task<IAuthzFilterResult> ApplyFilterForQuery(IQueryOperationContext context, AuthenticatedUser user, LayerSet readLayerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersQuery)
            {
                var r = await filter.FilterForQuery(context, user, readLayerIDs, trans, timeThreshold);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPreFilterForMutation(IPreMutationOperationContext context, AuthenticatedUser user, LayerSet readLayerIDs, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            // NOTE: we do not run any authz filters if the user is a super user
            // otherwise, a filter would be able to forbid the super user to do any action, which we don't want
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PreFilterForMutation(context, user, readLayerIDs, writeLayerID, trans, timeThreshold);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPostFilterForMutation(IPostMutationOperationContext context, AuthenticatedUser user, LayerSet readLayers, Changeset? changeset, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (PermissionUtils.HasSuperUserAuthRole(user))
                return AuthzFilterResultPermit.Instance;

            foreach (var filter in filtersMutation)
            {
                var r = await filter.PostFilterForMutation(context, user, readLayers, changeset, trans, timeThreshold);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }
    }
}