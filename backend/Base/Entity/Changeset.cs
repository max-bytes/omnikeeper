
using System;

namespace Landscape.Base.Entity
{

    public class Changeset
    {
        public Guid ID { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public UserInDatabase User { get; private set; }

        public static Changeset Build(Guid id, UserInDatabase user, DateTimeOffset timestamp)
        {
            var c = new Changeset
            {
                ID = id,
                User = user,
                Timestamp = timestamp
            };
            return c;
        }
    }
}
