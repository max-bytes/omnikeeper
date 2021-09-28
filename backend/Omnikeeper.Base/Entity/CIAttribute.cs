using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public enum AttributeState // TODO: remove, replace with boolean
    {
        New, Removed
    }

    public class MergedCIAttribute
    {
        public CIAttribute Attribute { get; private set; }
        public IList<string> LayerStackIDs { get; private set; }

        public MergedCIAttribute(CIAttribute attribute, IList<string> layerStackIDs)
        {
            Attribute = attribute;
            LayerStackIDs = layerStackIDs;
        }
    }

    [ProtoContract(SkipConstructor = true)]
    public class CIAttribute
    {
        [ProtoMember(1)] public readonly Guid ID;
        [ProtoMember(2)] public readonly string Name;
        [ProtoMember(3)] public readonly Guid CIID;
        [ProtoMember(4)] public readonly IAttributeValue Value;
        [ProtoMember(6)] public readonly Guid ChangesetID;

        // information hash: 
        public string InformationHash => CreateInformationHash(Name, CIID);
        public static string CreateInformationHash(string name, Guid ciid) => name + ciid;

        public CIAttribute(Guid id, string name, Guid CIID, IAttributeValue value, Guid changesetID)
        {
            ID = id;
            Name = name;
            this.CIID = CIID;
            Value = value;
            ChangesetID = changesetID;
        }
    }


    public interface IBulkCIAttributeData<F>
    {
        Guid GetCIID(F f);
        string NamePrefix { get; }
        string LayerID { get; }
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


            public Fragment(string name, IAttributeValue value, Guid ciid)
            {
                Name = name;
                Value = value;
                CIID = ciid;
            }
        }

        public string NamePrefix { get; private set; }
        public string LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataLayerScope(string namePrefix, string layerID, IEnumerable<Fragment> fragments)
        {
            NamePrefix = namePrefix;
            LayerID = layerID;
            Fragments = fragments.ToArray();
        }

        public static BulkCIAttributeDataLayerScope BuildFromDTO(BulkCIAttributeLayerScopeDTO dto)
        {
            return new BulkCIAttributeDataLayerScope(
                dto.NamePrefix, dto.LayerID,
                dto.Fragments.Select(f => new Fragment(f.Name, AttributeValueBuilder.BuildFromDTO(f.Value), f.CIID)));
        }
    }

    public class BulkCIAttributeDataCIScope : IBulkCIAttributeData<BulkCIAttributeDataCIScope.Fragment>
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public static string StripPrefix(string fullName, string prefix) => fullName.Replace($"{prefix}", "");

            public Fragment(string name, IAttributeValue value)
            {
                Name = name;
                Value = value;
            }
        }

        public string NamePrefix { get; private set; }
        public string LayerID { get; private set; }
        public Guid CIID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataCIScope(string namePrefix, string layerID, Guid ciid, IEnumerable<Fragment> fragments)
        {
            NamePrefix = namePrefix;
            LayerID = layerID;
            CIID = ciid;
            Fragments = fragments.ToArray();
        }
    }
}
