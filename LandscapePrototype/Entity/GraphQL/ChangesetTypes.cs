using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class UserTypeType : EnumerationGraphType<Entity.UserType>
    {
    }

    public class UserTypeGQL : ObjectGraphType<User>
    {
        public UserTypeGQL()
        {
            Field("id", x => x.ID);
            Field(x => x.Username);
            Field(x => x.Timestamp);
            Field("type", x => x.UserType, type: typeof(UserTypeType));
        }
    }

    public class ChangesetType : ObjectGraphType<Changeset>
    {
        public ChangesetType()
        {
            Field(x => x.Timestamp);
            Field(x => x.User, type: typeof(UserTypeGQL));
            Field("id", x => x.ID);
            //FieldAsync<UserTypeGQL>("user",
            //    resolve: async (context) =>
            //    { // TODO: refactor to have user be part of changeset -> no 1+N
            //        var userContext = context.UserContext as LandscapeUserContext;
            //        var userID = context.Source.UserID;
            //        var user = await userModel.GetUser(userID, userContext.Transaction);
            //        return user;
            //    });
        }
    }

}
