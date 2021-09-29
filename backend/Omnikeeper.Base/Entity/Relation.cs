using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public enum RelationState // TODO: remove
    {
        New, Removed
    }

    public class MergedRelation
    {
        public Relation Relation { get; private set; }
        public string[] LayerStackIDs { get; private set; }
        public string LayerID { get => LayerStackIDs[0]; }

        public MergedRelation(Relation relation, string[] layerStackIDs)
        {
            Relation = relation;
            LayerStackIDs = layerStackIDs;
        }
    }

    [ProtoContract(SkipConstructor = true)]
    public class Relation
    {
        [ProtoMember(1)] public readonly Guid ID;
        [ProtoMember(2)] public readonly Guid FromCIID;
        [ProtoMember(3)] public readonly Guid ToCIID;
        [ProtoMember(4)] public readonly string PredicateID;
        [ProtoMember(5)] public readonly Guid ChangesetID;

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(Guid fromCIID, Guid toCIID, string predicateID) => fromCIID + "_" + toCIID + "_" + predicateID;

        public Relation(Guid id, Guid fromCIID, Guid toCIID, string predicateID, Guid changesetID)
        {
            ID = id;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            PredicateID = predicateID;
            ChangesetID = changesetID;
        }
    }

    public interface IBulkRelationData<F>
    {
        public string LayerID { get; }
        public F[] Fragments { get; }

        string GetPredicateID(F fragment);
        Guid GetFromCIID(F fragment);
        Guid GetToCIID(F fragment);
    }

    public class BulkRelationDataPredicateScope : IBulkRelationData<BulkRelationDataPredicateScope.Fragment>
    {
        public class Fragment
        {
            public Guid From { get; private set; }
            public Guid To { get; private set; }

            public Fragment(Guid from, Guid to)
            {
                From = from;
                To = to;
            }
        }

        public string PredicateID { get; private set; }
        public string LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public BulkRelationDataPredicateScope(string predicateID, string layerID, IEnumerable<Fragment> fragments)
        {
            PredicateID = predicateID;
            LayerID = layerID;
            Fragments = fragments.ToArray();
        }
    }

    public class BulkRelationDataLayerScope : IBulkRelationData<BulkRelationDataLayerScope.Fragment>
    {
        public class Fragment
        {
            public Guid From { get; private set; }
            public Guid To { get; private set; }
            public string PredicateID { get; private set; }

            public Fragment(Guid from, Guid to, string predicateID)
            {
                From = from;
                To = to;
                PredicateID = predicateID;
            }
        }

        public string LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public BulkRelationDataLayerScope(string layerID, IEnumerable<Fragment> fragments)
        {
            LayerID = layerID;
            Fragments = fragments.ToArray();
        }
    }
}
