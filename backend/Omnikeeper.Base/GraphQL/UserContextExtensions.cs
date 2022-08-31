using GraphQL;

namespace Omnikeeper.Base.GraphQL
{
    public static class UserContextExtensions
    {
        public static OmnikeeperUserContext GetUserContext(this IResolveFieldContext rfc)
        {
            return (rfc.UserContext as OmnikeeperUserContext)!;
        }
    }
}
