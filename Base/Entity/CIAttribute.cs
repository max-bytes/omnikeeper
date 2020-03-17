
using LandscapePrototype.Entity.AttributeValues;
using System;

namespace LandscapePrototype.Entity
{
    public enum AttributeState
    {
        New, Changed, Removed, Renewed
    }

    public class MergedCIAttribute
    {
        public CIAttribute Attribute { get; private set; }
        public long[] LayerStackIDs { get; private set; }

        public static MergedCIAttribute Build(CIAttribute attribute, long[] layerStackIDs)
        {
            return new MergedCIAttribute
            {
                Attribute = attribute,
                LayerStackIDs = layerStackIDs
            };
        }
    }

    public class CIAttribute
    {
        public long ID { get; private set; }
        public string Name { get; private set; }
        public string CIID { get; private set; }
        public IAttributeValue Value { get; private set; }
        //public long LayerID { get => LayerStackIDs[^1]; }
        //public long[] LayerStackIDs { get; private set; }
        public AttributeState State { get; private set; }
        public long ChangesetID { get; private set; }

        // information hash: 
        public string InformationHash => CreateInformationHash(Name, CIID);
        public static string CreateInformationHash(string name, string ciid) => name + "_" + ciid;


        public static CIAttribute Build(long id, string name, string CIID, IAttributeValue value, AttributeState state, long changesetID)
        {
            return new CIAttribute
            {
                ID = id,
                Name = name,
                CIID = CIID,
                Value = value,
                //LayerStackIDs = layerStackIDs,
                State = state,
                ChangesetID = changesetID
            };
        }
    }

    public class BulkCIAttributeDataFragment
    {
        private string Name {  get; set; }
        public string FullName(string prefix) => $"{prefix}.{Name}";
        public IAttributeValue Value { get; private set; }
        public string CIID { get; private set; }

        public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}.", "");

        public static BulkCIAttributeDataFragment Build(string name, IAttributeValue value, string ciid)
        {
            return new BulkCIAttributeDataFragment()
            {
                Name = name,
                Value = value,
                CIID = ciid
            };
        }
    }

    public class BulkCIAttributeData
    {
        public string NamePrefix { get; private set; }
        public long LayerID { get; private set; }
        public BulkCIAttributeDataFragment[] Fragments { get; private set; }

        public static BulkCIAttributeData Build(string namePrefix, long layerID, BulkCIAttributeDataFragment[] fragments)
        {
            return new BulkCIAttributeData()
            {
                NamePrefix = namePrefix,
                LayerID = layerID,
                Fragments = fragments
            };
        }
    }
}
