using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilterForMutation
    {
        Task<IAuthzFilterResult> PreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs, IModelContext trans);
        Task<IAuthzFilterResult> PostFilterForMutation(MutationOperation operation, AuthenticatedUser user, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public interface IAuthzFilterForQuery
    {
        Task<IAuthzFilterResult> PreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IModelContext trans);
    }

    public interface IAuthzFilterResult
    {
    }
    public record class AuthzFilterResultDeny : IAuthzFilterResult
    {
        public AuthzFilterResultDeny(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; init; }
    }
    public class AuthzFilterResultPermit : IAuthzFilterResult
    {
        private AuthzFilterResultPermit() { }

        public static AuthzFilterResultPermit Instance = new AuthzFilterResultPermit();
    }

}
