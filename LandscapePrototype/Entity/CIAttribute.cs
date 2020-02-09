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
        public DateTime ActivationTime { get; private set; }
        public long LayerID { get; private set; }
        public AttributeState State { get; private set; }

        public static CIAttribute Build(string name, long CIID, IAttributeValue value, DateTime acticationTime, long layerID, AttributeState state)
        {
            var o = new CIAttribute();
            o.Name = name;
            o.CIID = CIID;
            o.Value = value;
            o.ActivationTime = acticationTime;
            o.LayerID = layerID;
            o.State = state;
            return o;
        }
    }
}
