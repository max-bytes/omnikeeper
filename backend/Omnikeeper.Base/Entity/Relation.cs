using Omnikeeper.Base.Entity.DataOrigin;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public enum RelationState
    {
        New, Removed, Renewed
    }

    public class MergedRelation
    {
        public Relation Relation { get; private set; }
        public long[] LayerStackIDs { get; private set; }
        public long LayerID { get => LayerStackIDs[^1]; }

        public MergedRelation(Relation relation, long[] layerStackIDs)
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
        public string PredicateID => Predicate.ID;
        [ProtoMember(4)] public readonly Predicate Predicate;
        [ProtoMember(5)] public readonly RelationState State;
        [ProtoMember(6)] public readonly Guid ChangesetID;
        [ProtoMember(7)] public readonly DataOriginV1 Origin;

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(Guid fromCIID, Guid toCIID, string predicateID) => fromCIID + "_" + toCIID + "_" + predicateID;

        public Relation(Guid id, Guid fromCIID, Guid toCIID, Predicate predicate, RelationState state, Guid changesetID, DataOriginV1 origin)
        {
            ID = id;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            Predicate = predicate;
            State = state;
            ChangesetID = changesetID;
            Origin = origin;
        }
    }

    public interface IBulkRelationData<F>
    {
        public long LayerID { get; }
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
        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public BulkRelationDataPredicateScope(string predicateID, long layerID, IEnumerable<Fragment> fragments)
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

        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public BulkRelationDataLayerScope(long layerID, IEnumerable<Fragment> fragments)
        {
            LayerID = layerID;
            Fragments = fragments.ToArray();
        }
    }
}
