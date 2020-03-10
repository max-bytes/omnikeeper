
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class Changeset
    {
        public long ID { get; private set; }
        public string Username { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public static Changeset Build(long id, string username, DateTimeOffset timestamp)
        {
            var c = new Changeset
            {
                ID = id,
                Username = username,
                Timestamp = timestamp
            };
            return c;
        }
    }
}
