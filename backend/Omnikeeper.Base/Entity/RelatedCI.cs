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
        public string PredicateWording { get; private set; }
        public Guid ChangesetID { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public bool IsForwardRelation { get; private set; }

        public CompactRelatedCI(CompactCI ci, Guid relationID, Guid fromCIID, Guid toCIID, Guid changesetID, string predicateID, bool isForwardRelation, string predicateWording, long[] layerStackIDs)
        {
            RelationID = relationID;
            CI = ci;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            ChangesetID = changesetID;
            PredicateID = predicateID;
            PredicateWording = predicateWording;
            LayerStackIDs = layerStackIDs;
            IsForwardRelation = isForwardRelation;
        }
    }


    public class MergedRelatedCI
    {
        public MergedCI CI { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public RelationState RelationState { get; private set; }

        public MergedRelatedCI(Relation r, Guid fromCIID, MergedCI ci)
        {
            CI = ci;
            FromCIID = fromCIID;
            ToCIID = ci.ID;
            PredicateID = r.PredicateID;
            RelationState = r.State;
        }
    }
}
