
using System;

namespace Landscape.Base.Entity
{
    public class UserInDatabase
    {
        public long ID { get; private set; }
        public Guid UUID { get; private set; }
        public string Username { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public UserType UserType { get; private set; }

        public static UserInDatabase Build(long id, Guid uuid, string username, UserType userType, DateTimeOffset timestamp)
        {
            var user = new UserInDatabase
            {
                ID = id,
                UUID = uuid,
                UserType = userType,
                Username = username,
                Timestamp = timestamp
            };
            return user;
        }
    }
}
