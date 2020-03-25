
using Landscape.Base.Model;
using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                State = state,
                ChangesetID = changesetID
            };
        }
    }

    public interface IBulkCIAttributeData<F>
    {
        string GetCIID(F f);
        string NamePrefix { get; }
        long LayerID { get; }
        public F[] Fragments { get; }

        string GetFullName(F fragment);
        IAttributeValue GetValue(F fragment);
    }

    public class BulkCIAttributeDataLayerScope : IBulkCIAttributeData<BulkCIAttributeDataLayerScope.Fragment>
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }
            public string CIID { get; private set; }

            public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}.", "");


            public static Fragment Build(string name, IAttributeValue value, string ciid)
            {
                return new Fragment()
                {
                    Name = name,
                    Value = value,
                    CIID = ciid
                };
            }
        }

        public string NamePrefix { get; private set; }
        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public string GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}.{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public static BulkCIAttributeDataLayerScope Build(string namePrefix, long layerID, IEnumerable<Fragment> fragments)
        {
            return new BulkCIAttributeDataLayerScope()
            {
                NamePrefix = namePrefix,
                LayerID = layerID,
                Fragments = fragments.ToArray()
            };
        }

    }

    public class BulkCIAttributeDataCIScope : IBulkCIAttributeData<BulkCIAttributeDataCIScope.Fragment>
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}.", "");

            public static Fragment Build(string name, IAttributeValue value)
            {
                return new Fragment()
                {
                    Name = name,
                    Value = value
                };
            }
        }

        public string NamePrefix { get; private set; }
        public long LayerID { get; private set; }
        public string CIID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public string GetCIID(Fragment f) => CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}.{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public static BulkCIAttributeDataCIScope Build(string namePrefix, long layerID, string ciid, IEnumerable<Fragment> fragments)
        {
            return new BulkCIAttributeDataCIScope()
            {
                NamePrefix = namePrefix,
                LayerID = layerID,
                CIID = ciid,
                Fragments = fragments.ToArray()
            };
        }
    }
}
