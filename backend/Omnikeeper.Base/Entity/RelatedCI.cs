using Omnikeeper.Base.Entity.DataOrigin;
using System;

namespace Omnikeeper.Base.Entity
{
    public class CompactRelatedCI
    {
        public CompactCI CI { get; private set; }
        public Guid RelationID { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public Guid ChangesetID { get; private set; }
        public DataOriginV1 Origin { get; private set; }
        public string LayerID { get => LayerStackIDs[^1]; }
        public string[] LayerStackIDs { get; private set; }
        public bool IsForwardRelation { get; private set; }

        public CompactRelatedCI(CompactCI ci, Guid relationID, Guid fromCIID, Guid toCIID, Guid changesetID, DataOriginV1 origin,
            string predicateID, bool isForwardRelation, string[] layerStackIDs)
        {
            RelationID = relationID;
            CI = ci;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            ChangesetID = changesetID;
            Origin = origin;
            PredicateID = predicateID;
            LayerStackIDs = layerStackIDs;
            IsForwardRelation = isForwardRelation;
        }
    }
}
