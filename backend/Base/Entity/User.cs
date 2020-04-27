using System.Collections.Generic;

namespace Landscape.Base.Entity
{
    public enum UserType
    {
        Human, Robot, Unknown
    }

    public class User
    {
        public UserInDatabase InDatabase { get; private set; }
        public IEnumerable<Layer> WritableLayers { get; private set; }

        public string Username => InDatabase.Username;

        public static User Build(UserInDatabase inDatabase, IEnumerable<Layer> writableLayers)
        {
            var user = new User
            {
                InDatabase = inDatabase,
                WritableLayers = writableLayers
            };
            return user;
        }
    }
}
