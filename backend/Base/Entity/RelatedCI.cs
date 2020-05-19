using System;

namespace Landscape.Base.Entity
{
    public class CompactRelatedCI
    {
        public CompactCI CI { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public string PredicateWording { get; private set; }
        public long ChangesetID { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public bool IsForwardRelation { get; private set; }

        public static CompactRelatedCI Build(CompactCI ci, Guid fromCIID, Guid toCIID, long changesetID, string predicateID, bool isForwardRelation, string predicateWording, long[] layerStackIDs)
        {
            var r = new CompactRelatedCI
            {
                CI = ci,
                FromCIID = fromCIID,
                ToCIID = toCIID,
                ChangesetID = changesetID,
                PredicateID = predicateID,
                PredicateWording = predicateWording,
                LayerStackIDs = layerStackIDs,
                IsForwardRelation = isForwardRelation
            };
            return r;
        }
    }


    public class MergedRelatedCI
    {
        public MergedCI CI { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get; private set; }
        public RelationState RelationState { get; private set; }

        public static MergedRelatedCI Build(Relation r, Guid fromCIID, MergedCI ci)
        {
            return new MergedRelatedCI
            {
                CI = ci,
                FromCIID = fromCIID,
                ToCIID = ci.ID,
                PredicateID = r.PredicateID,
                RelationState = r.State
            };
        }
    }
}
