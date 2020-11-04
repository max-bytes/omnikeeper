
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
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
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public Guid CIID { get; private set; }
        public IAttributeValue Value { get; private set; }
        public AttributeState State { get; private set; }
        public Guid ChangesetID { get; private set; }

        // information hash: 
        public string InformationHash => CreateInformationHash(Name, CIID);
        public static string CreateInformationHash(string name, Guid ciid) => name + "_" + ciid;


        public static CIAttribute Build(Guid id, string name, Guid CIID, IAttributeValue value, AttributeState state, Guid changesetID)
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
        Guid GetCIID(F f);
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
            public Guid CIID { get; private set; }

            public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}", "");


            public static Fragment Build(string name, IAttributeValue value, Guid ciid)
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

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
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

        public static BulkCIAttributeDataLayerScope BuildFromDTO(BulkCIAttributeLayerScopeDTO dto)
        {
            return Build(dto.NamePrefix, dto.LayerID, dto.Fragments.Select(f => Fragment.Build(f.Name, AttributeValueBuilder.BuildFromDTO(f.Value), f.CIID)));
        }
    }

    public class BulkCIAttributeDataCIScope : IBulkCIAttributeData<BulkCIAttributeDataCIScope.Fragment>
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}", "");

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
        public Guid CIID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public static BulkCIAttributeDataCIScope Build(string namePrefix, long layerID, Guid ciid, IEnumerable<Fragment> fragments)
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
