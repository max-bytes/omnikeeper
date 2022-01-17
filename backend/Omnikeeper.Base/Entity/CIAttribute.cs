using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
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

    //[ProtoContract(SkipConstructor = true)]
    public class CIAttribute
    {
        //[ProtoMember(1)] 
        public readonly Guid ID;
        //[ProtoMember(2)] 
        public readonly string Name;
        //[ProtoMember(3)] 
        public readonly Guid CIID;
        //[ProtoMember(4)] 
        public readonly IAttributeValue Value;
        //[ProtoMember(5)] 
        public readonly Guid ChangesetID;

        // information hash: 
        // TODO: move into extension method
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
        string LayerID { get; }
        public IEnumerable<F> Fragments { get; }

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

            public Fragment(string name, IAttributeValue value, Guid ciid)
            {
                Name = name;
                Value = value;
                CIID = ciid;
            }
        }

        public string NamePrefix { get; private set; }
        public string LayerID { get; private set; }
        public IEnumerable<Fragment> Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => $"{NamePrefix}{fragment.Name}";
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataLayerScope(string namePrefix, string layerID, IEnumerable<Fragment> fragments)
        {
            NamePrefix = namePrefix;
            LayerID = layerID;
            Fragments = fragments;
        }

        public static BulkCIAttributeDataLayerScope BuildFromDTO(BulkCIAttributeLayerScopeDTO dto)
        {
            return new BulkCIAttributeDataLayerScope(
                dto.NamePrefix, dto.LayerID,
                dto.Fragments.Select(f => new Fragment(f.Name, AttributeValueHelper.BuildFromDTO(f.Value), f.CIID)));
        }
    }

    public class BulkCIAttributeDataCIScope : IBulkCIAttributeData<BulkCIAttributeDataCIScope.Fragment>
    {
        public class Fragment
        {
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public Fragment(string name, IAttributeValue value)
            {
                Name = name;
                Value = value;
            }
        }

        public string LayerID { get; private set; }
        public Guid CIID { get; private set; }
        public IEnumerable<Fragment> Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => CIID;
        public string GetFullName(Fragment fragment) => fragment.Name;
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataCIScope(string layerID, Guid ciid, IEnumerable<Fragment> fragments)
        {
            LayerID = layerID;
            CIID = ciid;
            Fragments = fragments;
        }
    }


    public class BulkCIAttributeDataCIAndAttributeNameScope : IBulkCIAttributeData<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>
    {
        public ISet<Guid> RelevantCIs;

        public class Fragment
        {
            public Guid CIID { get; private set; }
            public string Name { get; private set; }
            public IAttributeValue Value { get; private set; }

            public Fragment(Guid ciid, string name, IAttributeValue value)
            {
                CIID = ciid;
                Name = name;
                Value = value;
            }
        }

        public string LayerID { get; private set; }
        public IEnumerable<Fragment> Fragments { get; private set; }
        public ISet<string> RelevantAttributes { get; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment f) => f.Name;
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataCIAndAttributeNameScope(string layerID, IEnumerable<Fragment> fragments, ISet<Guid> relevantCIs, ISet<string> relevantAttributes)
        {
            LayerID = layerID;
            Fragments = fragments;
            RelevantCIs = relevantCIs;
            RelevantAttributes = relevantAttributes;
        }
    }
}
