using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class AuthenticatedUser
    {
        public UserInDatabase InDatabase { get; private set; }
        public ISet<string> Permissions { get; private set; }

        public string Username => InDatabase.Username;

        public AuthenticatedUser(UserInDatabase inDatabase, ISet<string> permissions)
        {
            InDatabase = inDatabase;
            Permissions = permissions;
        }
    }
}
