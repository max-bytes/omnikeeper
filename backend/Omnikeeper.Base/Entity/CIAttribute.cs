using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public sealed class MergedCIAttribute
    {
        public CIAttribute Attribute { get; }
        public IList<string> LayerStackIDs { get; }

        public MergedCIAttribute(CIAttribute attribute, IList<string> layerStackIDs)
        {
            Attribute = attribute;
            LayerStackIDs = layerStackIDs;
        }
    }

    public sealed class CIAttribute
    {
        public readonly Guid ID;
        public readonly string Name;
        public readonly Guid CIID;
        public readonly IAttributeValue Value;
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

    public sealed class BulkCIAttributeDataLayerScope : IBulkCIAttributeData<BulkCIAttributeDataLayerScope.Fragment>
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
        public string LayerID { get; private set; }
        public IEnumerable<Fragment> Fragments { get; private set; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment fragment) => fragment.Name;
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataLayerScope(string layerID, IEnumerable<Fragment> fragments)
        {
            LayerID = layerID;
            Fragments = fragments;
        }
    }

    public sealed class BulkCIAttributeDataCIScope : IBulkCIAttributeData<BulkCIAttributeDataCIScope.Fragment>
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


    public sealed class BulkCIAttributeDataCIAndAttributeNameScope : IBulkCIAttributeData<BulkCIAttributeDataCIAndAttributeNameScope.Fragment>
    {
        public IReadOnlySet<Guid> RelevantCIs;

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
        public IReadOnlySet<string> RelevantAttributes { get; }

        public Guid GetCIID(Fragment f) => f.CIID;
        public string GetFullName(Fragment f) => f.Name;
        public IAttributeValue GetValue(Fragment f) => f.Value;

        public BulkCIAttributeDataCIAndAttributeNameScope(string layerID, IEnumerable<Fragment> fragments, IReadOnlySet<Guid> relevantCIs, IReadOnlySet<string> relevantAttributes)
        {
            LayerID = layerID;
            Fragments = fragments;
            RelevantCIs = relevantCIs;
            RelevantAttributes = relevantAttributes;
        }
    }
}
