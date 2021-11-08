using GraphQL;

namespace Omnikeeper.GraphQL
{
    public static class UserContextExtensions
    {
        public static OmnikeeperUserContext SetupUserContext(this IResolveFieldContext<object> rfc, bool partlyDisabled = false)
        {
            var userContext = (rfc.UserContext as OmnikeeperUserContext)!;
            userContext.PartlyDisabled = partlyDisabled;
            return userContext;
        }
    }
}
