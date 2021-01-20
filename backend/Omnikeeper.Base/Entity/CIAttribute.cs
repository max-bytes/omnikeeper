
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using ProtoBuf;
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

        public MergedCIAttribute(CIAttribute attribute, long[] layerStackIDs)
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
        [ProtoMember(5)] public readonly AttributeState State;
        [ProtoMember(6)] public readonly Guid ChangesetID;
        [ProtoMember(7)] public readonly DataOriginV1 Origin;

        // information hash: 
        public string InformationHash => CreateInformationHash(Name, CIID);
        public static string CreateInformationHash(string name, Guid ciid) => name + "_" + ciid;

        public CIAttribute(Guid id, string name, Guid CIID, IAttributeValue value, AttributeState state, Guid changesetID, DataOriginV1 origin)
        {
            ID = id;
            Name = name;
            this.CIID = CIID;
            Value = value;
            State = state;
            ChangesetID = changesetID;
            Origin = origin;
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


            public Fragment(string name, IAttributeValue value, Guid ciid)
            {
                Name = name;
                Value = value;
                CIID = ciid;
            }
        }

        public string NamePrefix { get; private set; }
        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataLayerScope(string namePrefix, long layerID, IEnumerable<Fragment> fragments)
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
        public long LayerID { get; private set; }
        public Guid CIID { get; private set; }
        public Fragment[] Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataCIScope(string namePrefix, long layerID, Guid ciid, IEnumerable<Fragment> fragments)
        {
            NamePrefix = namePrefix;
            LayerID = layerID;
            CIID = ciid;
            Fragments = fragments.ToArray();
        }
    }
}
