using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public sealed class MergedRelation
    {
        public Relation Relation { get; }
        public IList<string> LayerStackIDs { get; }

        public MergedRelation(Relation relation, IList<string> layerStackIDs)
        {
            Relation = relation;
            LayerStackIDs = layerStackIDs;
        }
    }

    public sealed class Relation
    {
        public readonly Guid ID;
        public readonly Guid FromCIID;
        public readonly Guid ToCIID;
        public readonly string PredicateID;
        public readonly Guid ChangesetID;

        public readonly bool Mask;

        // information hash: 
        public string InformationHash => CreateInformationHash(FromCIID, ToCIID, PredicateID);
        public static string CreateInformationHash(Guid fromCIID, Guid toCIID, string predicateID) => predicateID + fromCIID + toCIID;

        public Relation(Guid id, Guid fromCIID, Guid toCIID, string predicateID, Guid changesetID, bool mask)
        {
            ID = id;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            PredicateID = predicateID;
            ChangesetID = changesetID;
            Mask = mask;
        }
    }


    public class BulkRelationFullFragment
    {
        public Guid From { get; private set; }
        public Guid To { get; private set; }
        public string PredicateID { get; private set; }
        public bool Mask { get; private set; }

        public BulkRelationFullFragment(Guid from, Guid to, string predicateID, bool mask)
        {
            From = from;
            To = to;
            PredicateID = predicateID;
            Mask = mask;
        }
    }

    public interface IBulkRelationData<F>
    {
        public string LayerID { get; }
        public IEnumerable<F> Fragments { get; }

        string GetPredicateID(F fragment);
        Guid GetFromCIID(F fragment);
        Guid GetToCIID(F fragment);
        bool GetMask(F fragment);
    }

    public sealed class BulkRelationDataPredicateScope : IBulkRelationData<BulkRelationDataPredicateScope.Fragment>
    {
        public class Fragment
        {
            public Guid From { get; private set; }
            public Guid To { get; private set; }
            public bool Mask { get; private set; }

            public Fragment(Guid from, Guid to, bool mask)
            {
                From = from;
                To = to;
                Mask = mask;
            }
        }

        public string PredicateID { get; private set; }
        public string LayerID { get; private set; }
        public IEnumerable<Fragment> Fragments { get; private set; }
        public string GetPredicateID(Fragment fragment) => PredicateID;
        public Guid GetFromCIID(Fragment fragment) => fragment.From;
        public Guid GetToCIID(Fragment fragment) => fragment.To;
        public bool GetMask(Fragment fragment) => fragment.Mask;

        public BulkRelationDataPredicateScope(string predicateID, string layerID, IEnumerable<Fragment> fragments)
        {
            PredicateID = predicateID;
            LayerID = layerID;
            Fragments = fragments;
        }
    }

    public sealed class BulkRelationDataLayerScope : IBulkRelationData<BulkRelationFullFragment>
    {
        public string LayerID { get; private set; }
        public IEnumerable<BulkRelationFullFragment> Fragments { get; private set; }
        public string GetPredicateID(BulkRelationFullFragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(BulkRelationFullFragment fragment) => fragment.From;
        public Guid GetToCIID(BulkRelationFullFragment fragment) => fragment.To;
        public bool GetMask(BulkRelationFullFragment fragment) => fragment.Mask;

        public BulkRelationDataLayerScope(string layerID, IEnumerable<BulkRelationFullFragment> fragments)
        {
            LayerID = layerID;
            Fragments = fragments;
        }
    }

    public sealed class BulkRelationDataCIScope : IBulkRelationData<BulkRelationFullFragment>
    {
        public string LayerID { get; private set; }
        public IEnumerable<BulkRelationFullFragment> Fragments { get; private set; }
        public IReadOnlySet<Guid> RelevantCIIDs { get; }

        public string GetPredicateID(BulkRelationFullFragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(BulkRelationFullFragment fragment) => fragment.From;
        public Guid GetToCIID(BulkRelationFullFragment fragment) => fragment.To;
        public bool GetMask(BulkRelationFullFragment fragment) => fragment.Mask;

        public BulkRelationDataCIScope(string layerID, IEnumerable<BulkRelationFullFragment> fragments, IReadOnlySet<Guid> relevantCIIDs)
        {
            LayerID = layerID;
            Fragments = fragments;
            RelevantCIIDs = relevantCIIDs;
        }
    }

    public sealed class BulkRelationDataCIAndPredicateScope : IBulkRelationData<BulkRelationFullFragment>
    {
        public string LayerID { get; private set; }

        private readonly IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> Data;
        public readonly bool Outgoing;
        public readonly ISet<(Guid thisCIID, string predicateID)> Relevant;

        public string GetPredicateID(BulkRelationFullFragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(BulkRelationFullFragment fragment) => fragment.From;
        public Guid GetToCIID(BulkRelationFullFragment fragment) => fragment.To;
        public bool GetMask(BulkRelationFullFragment fragment) => fragment.Mask;

        public BulkRelationDataCIAndPredicateScope(string layerID, IList<(Guid thisCIID, string predicateID, Guid[] otherCIIDs)> data, ISet<(Guid thisCIID, string predicateID)> relevant, bool outgoing)
        {
            LayerID = layerID;
            Data = data;
            Outgoing = outgoing;
            Relevant = relevant;
        }

        public IEnumerable<BulkRelationFullFragment> Fragments
        {
            get
            {
                foreach (var (thisCIID, predicateID, otherCIIDs) in Data)
                {
                    foreach (var otherCIID in otherCIIDs)
                    {
                        var fromCIID = (Outgoing) ? thisCIID : otherCIID;
                        var toCIID = (Outgoing) ? otherCIID : thisCIID;
                        yield return new BulkRelationFullFragment(fromCIID, toCIID, predicateID, false); // TODO: add support for masks
                    }
                }
            }
        }
    }

    public sealed class BulkRelationDataSpecificScope : IBulkRelationData<BulkRelationFullFragment>
    {
        public string LayerID { get; private set; }
        public IEnumerable<BulkRelationFullFragment> Fragments { get; private set; }
        public string GetPredicateID(BulkRelationFullFragment fragment) => fragment.PredicateID;
        public Guid GetFromCIID(BulkRelationFullFragment fragment) => fragment.From;
        public Guid GetToCIID(BulkRelationFullFragment fragment) => fragment.To;
        public bool GetMask(BulkRelationFullFragment fragment) => fragment.Mask;

        public readonly IEnumerable<(Guid from, Guid to, string predicateID)> Removals;

        public BulkRelationDataSpecificScope(string layerID, IEnumerable<BulkRelationFullFragment> fragments, IEnumerable<(Guid from, Guid to, string predicateID)> removals)
        {
            LayerID = layerID;
            Fragments = fragments;
            Removals = removals;
        }
    }
}
