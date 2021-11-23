using GraphQL;

namespace Omnikeeper.GraphQL
{
    public static class UserContextExtensions
    {
        public static OmnikeeperUserContext SetupUserContext(this IResolveFieldContext<object> rfc)
        {
            var userContext = (rfc.UserContext as OmnikeeperUserContext)!;
            return userContext;
        }
    }
}
