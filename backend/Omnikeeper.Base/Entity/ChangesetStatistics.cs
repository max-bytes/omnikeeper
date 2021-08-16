
using System;

namespace Omnikeeper.Base.Entity
{
    public class ChangesetStatistics
    {
        public ChangesetStatistics(Guid changesetID, long numAttributeChanges, long numRelationChanges)
        {
            ChangesetID = changesetID;
            NumAttributeChanges = numAttributeChanges;
            NumRelationChanges = numRelationChanges;
        }

        public Guid ChangesetID { get; private set; }
        public long NumAttributeChanges { get; private set; }
        public long NumRelationChanges { get; private set; }

    }
}
