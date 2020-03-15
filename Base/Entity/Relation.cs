using LandscapePrototype.Entity.AttributeValues;
using System;

namespace LandscapePrototype.Entity
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

        public static Relation Build(long id, string fromCIID, string toCIID, Predicate predicate, long[] layerStackIDs, RelationState state, long changesetID)
        {
            var o = new Relation();
            o.ID = id;
            o.FromCIID = fromCIID;
            o.ToCIID = toCIID;
            o.Predicate = predicate;
            o.LayerStackIDs = layerStackIDs;
            o.State = state;
            o.ChangesetID = changesetID;
            return o;
        }
    }
}
