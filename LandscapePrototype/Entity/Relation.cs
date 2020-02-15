using LandscapePrototype.Entity.AttributeValues;
using System;

namespace LandscapePrototype
{
    public enum RelationState
    {
        New, Removed, Renewed
    }

    public class Relation
    {
        public long FromCIID { get; private set; }
        public long ToCIID { get; private set; }
        public string Predicate { get; private set; }
        public DateTimeOffset ActivationTime { get; private set; }
        public long LayerID { get; private set; }
        public RelationState State { get; private set; }
        public long ChangesetID { get; private set; }

        public static Relation Build(long fromCIID, long toCIID, string predicate, DateTimeOffset acticationTime, long layerID, RelationState state, long changesetID)
        {
            var o = new Relation();
            o.FromCIID = fromCIID;
            o.ToCIID = toCIID;
            o.Predicate = predicate;
            o.ActivationTime = acticationTime;
            o.LayerID = layerID;
            o.State = state;
            o.ChangesetID = changesetID;
            return o;
        }
    }
}
