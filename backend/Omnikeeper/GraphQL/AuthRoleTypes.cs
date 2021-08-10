using GraphQL.Types;
using Omnikeeper.Base.Entity;
using System;
using System.Linq;

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
