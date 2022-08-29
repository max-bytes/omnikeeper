using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilter
    {
        Task<IAuthzFilterResult> PreFilterForMutation(MutationOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs, IEnumerable<string> writeLayerIDs);
        Task<IAuthzFilterResult> PostFilterForMutation(MutationOperation operation, AuthenticatedUser user, IChangesetProxy changesetProxy);

        Task<IAuthzFilterResult> PreFilterForQuery(QueryOperation operation, AuthenticatedUser user, IEnumerable<string> readLayerIDs);
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
