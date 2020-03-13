
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class User
    {
        public long ID { get; private set; }
        public Guid UUID { get; private set; }
        public string Username { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public static User Build(long id, Guid uuid, string username, DateTimeOffset timestamp)
        {
            var user = new User
            {
                ID = id,
                UUID = uuid,
                Username = username,
                Timestamp = timestamp
            };
            return user;
        }
    }
}
