using LandscapePrototype.Entity.AttributeValues;
using System;

namespace LandscapePrototype
{
    public enum AttributeState
    {
        New, Changed, Removed, Renewed
    }

    public class CIAttribute
    {
        public string Name { get; private set; }
        public long CIID { get; private set; }
        public IAttributeValue Value { get; private set; }
        public DateTimeOffset ActivationTime { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }
        public long[] LayerStackIDs { get; private set; }
        public AttributeState State { get; private set; }
        public long ChangesetID { get; private set; }

        public static CIAttribute Build(string name, long CIID, IAttributeValue value, DateTimeOffset acticationTime, long[] layerStackIDs, AttributeState state, long changesetID)
        {
            var o = new CIAttribute();
            o.Name = name;
            o.CIID = CIID;
            o.Value = value;
            o.ActivationTime = acticationTime;
            o.LayerStackIDs = layerStackIDs;
            o.State = state;
            o.ChangesetID = changesetID;
            return o;
        }
    }
}
