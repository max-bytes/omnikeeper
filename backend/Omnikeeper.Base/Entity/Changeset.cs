
using Omnikeeper.Base.Entity.DataOrigin;
using System;

namespace Omnikeeper.Base.Entity
{
    public class Changeset
    {
        public Guid ID { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string LayerID { get; private set; }
        public DataOriginV1 DataOrigin { get; }
        public UserInDatabase User { get; private set; }

        public Changeset(Guid id, UserInDatabase user, string layerID, DataOriginV1 dataOrigin, DateTimeOffset timestamp)
        {
            ID = id;
            User = user;
            LayerID = layerID;
            DataOrigin = dataOrigin;
            Timestamp = timestamp;
        }
    }
}
