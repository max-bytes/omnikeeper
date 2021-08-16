
using System;

namespace Omnikeeper.Base.Entity
{
    public class Changeset
    {
        public Guid ID { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string LayerID { get; private set; }
        public UserInDatabase User { get; private set; }

        public Changeset(Guid id, UserInDatabase user, string layerID, DateTimeOffset timestamp)
        {
            ID = id;
            User = user;
            LayerID = layerID;
            Timestamp = timestamp;
        }
    }
}
