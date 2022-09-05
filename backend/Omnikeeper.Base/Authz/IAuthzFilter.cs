using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Authz
{
    public interface IAuthzFilter { }

    public interface IAuthzFilterForMutation : IAuthzFilter
    {
        Task<IAuthzFilterResult> PreFilterForMutation(IPreMutationOperationContext context, IAuthenticatedUser user, LayerSet readLayers, string writeLayerID, IModelContext trans, TimeThreshold timeThreshold);
        Task<IAuthzFilterResult> PostFilterForMutation(IPostMutationOperationContext context, IAuthenticatedUser user, LayerSet readLayers, Changeset? changeset, IModelContext trans, TimeThreshold timeThreshold);
    }

    public interface IAuthzFilterForQuery : IAuthzFilter
    {
        Task<IAuthzFilterResult> FilterForQuery(IQueryOperationContext context, IAuthenticatedUser user, LayerSet readLayers, IModelContext trans, TimeThreshold timeThreshold);
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
