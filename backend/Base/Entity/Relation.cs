namespace Landscape.Base.Entity
{
    public enum RelationState
    {
        New, Removed, Renewed
    }

    public class Relation
    {
        public long ID { get; private set; }
        public string FromCIID { get; private set; }
        public string ToCIID { get; private set; }
        public string PredicateID { get => Predicate.ID; }
        public Predicate Predicate { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public RelationState State { get; private set; }
        public long ChangesetID { get; private set; }

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(string fromCIID, string toCIID, string predicateID) => fromCIID + "_" + toCIID + "_" + predicateID;

        public static Relation Build(long id, string fromCIID, string toCIID, Predicate predicate, long[] layerStackIDs, RelationState state, long changesetID)
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
        public (string, string)[] FromToCIIDPairs { get; private set; } // TODO: create and refactor into BulkRelationDataFragment

        public static BulkRelationData Build(string predicateID, long layerID, (string, string)[] fromToCIIDPairs)
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
