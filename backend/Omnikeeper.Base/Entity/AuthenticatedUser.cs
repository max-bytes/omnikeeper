using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class AuthenticatedUser
    {
        public UserInDatabase InDatabase { get; private set; }
        public AuthRole[] AuthRoles { get; private set; }

        public string Username => InDatabase.Username;

        public AuthenticatedUser(UserInDatabase inDatabase, AuthRole[] authRoles)
        {
            InDatabase = inDatabase;
            AuthRoles = authRoles;
        }
    }
}
