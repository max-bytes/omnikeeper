using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class Changeset
    {
        public long ID { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public static Changeset Build(long id, DateTimeOffset timestamp)
        {
            var c = new Changeset
            {
                ID = id,
                Timestamp = timestamp
            };
            return c;
        }
    }
}
