using Omnikeeper.Base.Authz;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Authz
{
    public class AuthzFilterManager : IAuthzFilterManager
    {
        private readonly IEnumerable<IAuthzFilter> filters;

        public AuthzFilterManager(IEnumerable<IAuthzFilter> filters)
        {
            this.filters = filters;
        }

        public async Task<IAuthzFilterResult> ApplyPreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs)
        {
            foreach (var filter in filters)
            {
                var r = await filter.PreFilterForMutation(operation, user, readLayerIDs, writeLayerIDs);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }

        public async Task<IAuthzFilterResult> ApplyPreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs)
        {
            foreach (var filter in filters)
            {
                var r = await filter.PreFilterForQuery(operation, user, readLayerIDs);
                if (r is AuthzFilterResultDeny d)
                    return d;
            }
            return AuthzFilterResultPermit.Instance;
        }
    }
}
