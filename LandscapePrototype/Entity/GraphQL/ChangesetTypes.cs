using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field("id", x => x.ID);
            Field(x => x.Username);
            Field(x => x.Timestamp);
        }
    }

    public class ChangesetType : ObjectGraphType<Changeset>
    {
        public ChangesetType(UserModel userModel)
        {
            Field(x => x.Timestamp);
            Field(x => x.UserID);
            Field("id", x => x.ID);
            FieldAsync<UserType>("user",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
                var userID = context.Source.UserID;
                var user = await userModel.GetUser(userID, userContext.Transaction);
                return user;
            });
        }
    }

}
