using System.Collections.Generic;

namespace Landscape.Base.Entity
{
    public class AuthenticatedUser
    {
        public UserInDatabase InDatabase { get; private set; }
        public IEnumerable<Layer> WritableLayers { get; private set; }

        public string Username => InDatabase.Username;

        public static AuthenticatedUser Build(UserInDatabase inDatabase, IEnumerable<Layer> writableLayers)
        {
            var user = new AuthenticatedUser
            {
                InDatabase = inDatabase,
                WritableLayers = writableLayers
            };
            return user;
        }
    }
}
