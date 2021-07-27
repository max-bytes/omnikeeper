using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class AuthenticatedUser
    {
        public UserInDatabase InDatabase { get; private set; }
        public string AccessToken { get; }
        public IEnumerable<Layer> WritableLayers { get; private set; }

        public string Username => InDatabase.Username;

        public AuthenticatedUser(UserInDatabase inDatabase, string accessToken, IEnumerable<Layer> writableLayers)
        {
            InDatabase = inDatabase;
            AccessToken = accessToken;
            WritableLayers = writableLayers;
        }
    }
}
