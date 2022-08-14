using GraphQL;

namespace Omnikeeper.GraphQL
{
    public static class UserContextExtensions
    {
        public static OmnikeeperUserContext GetUserContext(this IResolveFieldContext rfc)
        {
            return (rfc.UserContext as OmnikeeperUserContext)!;
        }
    }
}
