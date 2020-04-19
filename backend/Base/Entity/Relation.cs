using System;

namespace Landscape.Base.Entity
{
    public enum RelationState
    {
        New, Removed, Renewed
    }

    public class Relation
    {
        public long ID { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get => Predicate.ID; }
        public Predicate Predicate { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public RelationState State { get; private set; }
        public long ChangesetID { get; private set; }

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(Guid fromCIID, Guid toCIID, string predicateID) => fromCIID + "_" + toCIID + "_" + predicateID;

        public static Relation Build(long id, Guid fromCIID, Guid toCIID, Predicate predicate, long[] layerStackIDs, RelationState state, long changesetID)
        {
            return new Relation
            {
                ID = id,
                FromCIID = fromCIID,
                ToCIID = toCIID,
                Predicate = predicate,
                LayerStackIDs = layerStackIDs,
                State = state,
                ChangesetID = changesetID
            };
        }
    }

    public class BulkRelationData
    {
        public string PredicateID { get; private set; }
        public long LayerID { get; private set; }
        public (Guid, Guid)[] FromToCIIDPairs { get; private set; } // TODO: create and refactor into BulkRelationDataFragment

        public static BulkRelationData Build(string predicateID, long layerID, (Guid, Guid)[] fromToCIIDPairs)
        {
            return new BulkRelationData()
            {
                PredicateID = predicateID,
                LayerID = layerID,
                FromToCIIDPairs = fromToCIIDPairs
            };
        }
    }
}
