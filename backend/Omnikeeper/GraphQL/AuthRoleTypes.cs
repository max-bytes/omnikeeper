using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL
{
    public class AuthRoleType : ObjectGraphType<AuthRole>
    {
        public AuthRoleType()
        {
            Field("id", x => x.ID);
            Field(x => x.Permissions);
        }
    }
}
