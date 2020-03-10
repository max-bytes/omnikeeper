
using LandscapePrototype.Entity.AttributeValues;
using System;

namespace LandscapePrototype.Entity
{
    public enum AttributeState
    {
        New, Changed, Removed, Renewed
    }

    public class CIAttribute
    {
        public long ID { get; private set; }
        public string Name { get; private set; }
        public string CIID { get; private set; }
        public IAttributeValue Value { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public AttributeState State { get; private set; }
        public long ChangesetID { get; private set; }

        public static CIAttribute Build(long id, string name, string CIID, IAttributeValue value, long[] layerStackIDs, AttributeState state, long changesetID)
        {
            var o = new CIAttribute();
            o.ID = id;
            o.Name = name;
            o.CIID = CIID;
            o.Value = value;
            o.LayerStackIDs = layerStackIDs;
            o.State = state;
            o.ChangesetID = changesetID;
            return o;
        }
    }
}
