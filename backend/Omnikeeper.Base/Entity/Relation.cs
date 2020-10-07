using System;

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

        public static MergedRelation Build(Relation relation, long[] layerStackIDs)
        {
            return new MergedRelation
            {
                Relation = relation,
                LayerStackIDs = layerStackIDs
            };
        }
    }

    public class Relation
    {
        public Guid ID { get; private set; }
        public Guid FromCIID { get; private set; }
        public Guid ToCIID { get; private set; }
        public string PredicateID { get => Predicate.ID; }
        public Predicate Predicate { get; private set; }
        public RelationState State { get; private set; }
        public Guid ChangesetID { get; private set; }

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(Guid fromCIID, Guid toCIID, string predicateID) => fromCIID + "_" + toCIID + "_" + predicateID;

        public static Relation Build(Guid id, Guid fromCIID, Guid toCIID, Predicate predicate, RelationState state, Guid changesetID)
        {
            return new Relation
            {
                ID = id,
                FromCIID = fromCIID,
                ToCIID = toCIID,
                Predicate = predicate,
                State = state,
                ChangesetID = changesetID
            };
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

            public static Fragment Build(Guid from, Guid to)
            {
                return new Fragment()
                {
                    From = from,
                    To = to
                };
            }
        }

        public string PredicateID { get; private set; }
        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public static BulkRelationDataPredicateScope Build(string predicateID, long layerID, Fragment[] fragments)
        {
            return new BulkRelationDataPredicateScope()
            {
                PredicateID = predicateID,
                LayerID = layerID,
                Fragments = fragments
            };
        }
    }

    public class BulkRelationDataLayerScope : IBulkRelationData<BulkRelationDataLayerScope.Fragment>
    {
        public class Fragment
        {
            public Guid From { get; private set; }
            public Guid To { get; private set; }
            public string PredicateID { get; private set; }

            public static Fragment Build(Guid from, Guid to, string predicateID)
            {
                return new Fragment()
                {
                    From = from,
                    To = to,
                    PredicateID = predicateID
                };
            }
        }

        public long LayerID { get; private set; }
        public Fragment[] Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;

        public static BulkRelationDataLayerScope Build(long layerID, Fragment[] fragments)
        {
            return new BulkRelationDataLayerScope()
            {
                LayerID = layerID,
                Fragments = fragments
            };
        }
    }
}
