using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class AuthenticatedUser
    {
        public UserInDatabase InDatabase { get; private set; }
        public IEnumerable<string> Permissions { get; private set; }

        public string Username => InDatabase.Username;

        public AuthenticatedUser(UserInDatabase inDatabase, IEnumerable<string> permissions)
        {
            InDatabase = inDatabase;
            Permissions = permissions;
        }
    }
}
